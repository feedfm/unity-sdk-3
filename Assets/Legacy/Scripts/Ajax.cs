using System;
using System.Collections;
using System.Collections.Generic;
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

namespace Legacy.Scripts
{
   public class Ajax {
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

      private UnityWebRequest request;
   

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

      public void addParameter(string name, string value)
      {
         fields.Add(name, value);
      }

      public void addHeader(string name, string value)
      {
         headers.Add(name, value);
      }

      public IEnumerator Request()
      {

         fields.Add("force200", "1");

         if (type == RequestType.POST) {
            WWWForm form = new WWWForm();
            if (fields.Count == 0) {
               form.AddField("ju", "nk");
            } else {
               foreach (KeyValuePair <string, string> kp in fields) {
                  form.AddField(kp.Key, kp.Value);
               }
            }
            //setRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            request = UnityWebRequest.Post(url, form);
            //request.SetRequestHeader("content-type", "multipart/form-data;");
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            foreach (KeyValuePair <string, string> kp in headers) {
               request.SetRequestHeader(kp.Key, kp.Value);
            }
         } else {
            if (fields != null) {
               url = url + ToQueryString(fields);
            }
            request = UnityWebRequest.Get(url);
            foreach (KeyValuePair <string, string> kp in headers) {
               request.SetRequestHeader(kp.Key, kp.Value);
            }
         }
         request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
         yield return request.SendWebRequest();

         if ( request.result == UnityWebRequest.Result.ConnectionError ) {
            //Debug.Log(request.error);
            success		= false;
            error			= 500;
            errorMessage	= request.downloadHandler.text;
            yield break;
         } 
#if !UNITY_PS4 || UNITY_EDITOR
         else if (request.result == UnityWebRequest.Result.ConnectionError) {
            //Debug.Log(request.error);
            success		= false;
            error			= 500;
            errorMessage	= request.downloadHandler.text;
            yield break;
         } 
#endif
         else {
            try {
               // all responses should be JSON
               response = JSONNode.Parse(request.downloadHandler.text);
               if (response["success"].AsBool) {
                  success = true;
                  yield break;
               } else {
                  success		= false;
                  error		= response["error"]["code"].AsInt;
                  errorMessage = response["error"]["message"];

                  yield break;
               }
            } catch(Exception) {
               success = false;
               yield break;
            }
         }
      }

      private string ToQueryString(Dictionary <string, string> nvc)
      {
         string query = "";

         foreach (KeyValuePair <string, string> pair in nvc) {
            if (query.Length > 0) {
               query += "&";
            } else {
               query += "?";
            }

            query += string.Format("{0}={1}", UnityWebRequest.EscapeURL(pair.Key), UnityWebRequest.EscapeURL(pair.Value));
         }

         return query;
      }

      public void DebugResponse()
      {
         if (success) {
            Debug.Log("Request:" + request.url +"\n"+ "Response: " + request.downloadHandler.text);
         } else {
            Debug.Log("Request:" + request.url +"\n"+ "Error id " + error + ", Response: " + request.downloadHandler.text);
         }
      }
   }
}

