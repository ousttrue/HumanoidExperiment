using System.Collections;
using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;
using UnityEngine.Playables;


namespace HumanoidExperiment
{
    public class JobTest : MonoBehaviour
    {
        HumanPoseTransfer m_source;
        public HumanPoseTransfer Transfer
        {
            get { return m_source; }
            set
            {
                if (m_source == value) return;

                enabled = false;
                m_source = value;
                if (m_source != null)
                {
                    enabled = true;
                }
            }
        }

        PlayableGraph m_graph;

        private void Awake()
        {
            // ceate dummy human
        }

        private void OnEnable()
        {
            if (m_source == null)
            {
                return;
            }

            m_graph = PlayableGraph.Create();
            m_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        }

        private void OnDisable()
        {
            if (m_graph.IsValid())
            {
                m_graph.Destroy();
            }
        }
    }
}
