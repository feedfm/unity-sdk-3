using System;
using System.Collections;
using System.Collections.Generic;
using FeedFM.Extensions;
using FeedFM.Models;
using UnityEngine;
using UnityEngine.Networking;

/*
 * Wrapper for the crappy WWW and WWWForm classes. This class
 * takes care of formatting parameters properly and parsing out
 * the JSON responses that feed.fm returns.
 *
 * This might make a good general purpose wrapper for WWW if
 * we didn't have the 'success' and jSON logic in it.
 *
 */

namespace FeedFM.Utilities
{
   public class Ajax
   {
      private string _requestDownloadHandlerText = string.Empty;
      
      /*
	 * Request parameters
	 */

      public enum RequestType {GET, POST};

      public string url
      {
         get;
         private set;
      }

      public RequestType type
      {
         get;
         private set;
      }

      private Dictionary <string, string> fields	= new Dictionary <string, string>();
      private Dictionary <string, string> headers	= new Dictionary <string, string>();

      // private UnityWebRequest request;
   

      /*
	 * Response data
	 */

      public JSONNode response
      {
         get;
         private set;
      }

      public bool success
      {
         get;
         private set;
      }

      public int error
      {
         get;
         private set;
      }

      public string errorMessage
      {
         get;
         private set;
      }

      public Ajax(RequestType type, string url)
      {
         this.type = type;
         this.url	= url;
      }

      public Ajax addParameter(string name, string value)
      {
         fields.Add(name, value);
         return this;
      }

      public void addHeader(string name, string value)
      {
         headers.Add(name, value);
      }

      public IEnumerator Request()
      {
         UnityWebRequest request = null;
         
         try
         {
            fields.Add("force200", "1");

            if (type == RequestType.POST)
            {
               WWWForm form = new WWWForm();
               if (fields.Count == 0)
               {
                  form.AddField("ju", "nk");
               }
               else
               {
                  foreach (KeyValuePair<string, string> kp in fields)
                  {
                     form.AddField(kp.Key, kp.Value);
                  }
               }

               //setRequestHeader("Content-Type", "application/x-www-form-urlencoded");
               request = UnityWebRequest.Post(url, form);
               //request.SetRequestHeader("content-type", "multipart/form-data;");
               request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
               foreach (KeyValuePair<string, string> kp in headers)
               {
                  request.SetRequestHeader(kp.Key, kp.Value);
               }
            }
            else
            {
               if (fields != null)
               {
                  url = $"{url}{fields.ToQueryString()}";
               }

               request = UnityWebRequest.Get(url);
               foreach (KeyValuePair<string, string> kp in headers)
               {
                  request.SetRequestHeader(kp.Key, kp.Value);
               }
            }

            request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            _requestDownloadHandlerText = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
               //Debug.Log(request.error);
               success = false;
               error = 500;
               errorMessage = _requestDownloadHandlerText;
               yield break;
            }
#if !UNITY_PS4 || UNITY_EDITOR
            else if (request.result == UnityWebRequest.Result.ConnectionError)
            {
               //Debug.Log(request.error);
               success = false;
               error = 500;
               errorMessage = _requestDownloadHandlerText;
               yield break;
            }
#endif
            else
            {
               try
               {
                  // all responses should be JSON
                  response = JSONNode.Parse(_requestDownloadHandlerText);
                  if (response["success"].AsBool)
                  {
                     success = true;
                     error = (int) FeedError.None;
                     yield break;
                  }
                  else
                  {
                     success = false;
                     error = response["error"]["code"].AsInt;
                     errorMessage = response["error"]["message"];

                     yield break;
                  }
               }
               catch (Exception ex)
               {
                  success = false;
                  error = (int) FeedError.UndefinedError;
                  errorMessage = ex.Message;
                  yield break;
               }
            }
         }
         finally
         {
            if (request != null)
            {
               request.Dispose();
            }
         }
      }

      public void DebugResponse()
      {
#if UNITY_EDITOR
         if (success) {
            Debug.Log(string.Format("Request:{0}\n" + "Response: {1}", url, _requestDownloadHandlerText));
         } else {
            Debug.Log(string.Format("Request:{0}\n" + "Error id {1}, Response: {2}", url, error, _requestDownloadHandlerText));
         }
#endif
      }
   }
}

