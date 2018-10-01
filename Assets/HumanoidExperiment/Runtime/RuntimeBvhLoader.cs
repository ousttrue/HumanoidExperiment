using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniHumanoid;
using System.IO;
using System;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace HumanoidExperiment
{
    public class RuntimeBvhLoader : MonoBehaviour
    {
        [SerializeField]
        Button m_openButton;

        [SerializeField]
        HumanPoseTransfer m_dst;

        HumanPoseTransfer m_src;

        UnityAction m_onClick;

        private void Awake()
        {
            m_onClick = new UnityEngine.Events.UnityAction(OnClick);
        }

        private void OnEnable()
        {
            m_openButton.onClick.AddListener(m_onClick);
        }

        private void OnDisable()
        {
            m_openButton.onClick.RemoveListener(m_onClick);
        }

        static string m_lastDir;

        public void OnClick()
        {
#if UNITY_EDITOR
            var path = EditorUtility.OpenFilePanel("open bvh", m_lastDir, "bvh");
            if (String.IsNullOrEmpty(path))
            {
                return;
            }
            m_lastDir = Path.GetDirectoryName(path);
#else
            string path=null;
            throw new NotImplementedException();
#endif

#pragma warning disable 4014
            Open(path);
#pragma warning restore 4014
        }

        BvhImporterContext m_context;

        async Task Open(string path)
        {
            Debug.LogFormat("Open: {0}", path);
            if (m_context != null)
            {
                m_context.Destroy(true);
                m_context = null;
            }

            m_context = new BvhImporterContext();

            await Task.Run(() =>
            {
                m_context.Parse(path);
            });

            m_context.Load();

            m_src = m_context.Root.AddComponent<HumanPoseTransfer>();

            m_dst.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
            m_dst.Source = m_src;
        }
    }
}
