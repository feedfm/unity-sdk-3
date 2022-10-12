using System;
using UnityEngine;

namespace FeedFM
{
    [RequireComponent(typeof(MixingAudioPlayer))]
    [RequireComponent(typeof(Session))]
    [DisallowMultipleComponent]
    internal sealed class FeedPlayer : MonoBehaviour
    {
        [SerializeField] private MixingAudioPlayer _mixingAudioPlayer = null;
        [SerializeField] private Session _session = null;
        
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
            UnityEditorInternal.ComponentUtility.MoveComponentUp(this);
            UnityEditorInternal.ComponentUtility.MoveComponentUp(this);
            
            InitializeRequiredComponents();
        }
#endif
    }
}