using System;
using FeedFM.Attributes;
using UnityEngine;

namespace FeedFM
{
    [RequireComponent(typeof(MixingAudioPlayer))]
    [RequireComponent(typeof(Session))]
    [DisallowMultipleComponent]
    internal sealed class FeedPlayer : MonoBehaviour
    {
        [SerializeField, ReadOnly] private MixingAudioPlayer _mixingAudioPlayer = null;
        [SerializeField, ReadOnly] private Session _session = null;
        
        private void Awake()
        {
            InitializeRequiredComponents();
        }

        private void InitializeRequiredComponents()
        {
            if (!_mixingAudioPlayer)
            {
                _mixingAudioPlayer = GetComponent<MixingAudioPlayer>();
                if (!_mixingAudioPlayer)
                {
                    _mixingAudioPlayer = gameObject.AddComponent<MixingAudioPlayer>();
                }
            }

            if (!_session)
            {
                _session = GetComponent<Session>();
                if (!_session)
                {
                    _session = gameObject.AddComponent<Session>();
                }
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            InitializeRequiredComponents();
            
            while (UnityEditorInternal.ComponentUtility.MoveComponentUp(this))
            {
                // Move AudioSource component to bottom
            }
            
            if (_mixingAudioPlayer)
            {
                
                _mixingAudioPlayer.Reset();
            }
        }
#endif
    }
}