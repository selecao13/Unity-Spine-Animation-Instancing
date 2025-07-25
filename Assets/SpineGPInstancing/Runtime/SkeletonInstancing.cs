using UnityEngine;

namespace Spine.Instancing
{

    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class SkeletonInstancing : MonoBehaviour
    {

        public string animationName
        {
            get
            {
                if (!valid)
                {
                    return m_animationName;
                }
                else
                {
                    if (m_animationSate != null)
                    {
                        var entry = m_animationSate.GetCurrent();
                        return entry == null ? null : entry.animation.name;
                    }
                    return null;
                }
            }
            set
            {
                Initialize(false);
                if (m_animationName == value)
                {
                    TrackEntry entry = animationSate.GetCurrent();
                    if (entry != null && entry.animation.name == value)
                        return;
                }
                m_animationName = value;
                if (string.IsNullOrEmpty(value)) 
                {
                    animationSate.ClearTrack();
                }
                else
                {
                    var animation = instanceData.FindAnimation(m_animationName);
                    m_animationSate.SetAnimation(animation, loop);
                }
            }
        }

        [SerializeField]
        public SkeletonInstancingDataAsset dataAsset;

        public SkeletonInstancingData instanceData;
        public string startingAnimation { get; set; }
        public bool startingLoop { get; set; }
        public Spine.Instancing.AnimationState animationSate { get { return m_animationSate; } }

        public bool loop = false;
        public float timeScale = 1;
        public bool unscaledTime = false;

        [SerializeField]
        private string m_animationName;
        [SerializeField]
        private bool m_flipX = false;
        private Spine.Instancing.AnimationState m_animationSate;
        private MeshRenderer m_meshRenderer;

        [System.NonSerialized] public bool valid = false;

        private void Awake()
        {
            Initialize(false);
        }

#if UNITY_EDITOR
        private void Start()
        {
            Initialize(false);
        }
#endif

        public void Initialize(bool overwrite)
        {
            if (!overwrite && valid)
            {
                return;
            }
            valid = false;
            if (dataAsset == null || dataAsset.GetSkeletonInstancingData() == null)
            {
                return;
            }
            instanceData = dataAsset.GetSkeletonInstancingData();;
            m_meshRenderer = GetComponent<MeshRenderer>();
            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = instanceData.sharedMesh;
            m_meshRenderer.sharedMaterial = instanceData.sharedMaterial;
            startingLoop = loop;
            m_animationSate = new Spine.Instancing.AnimationState(instanceData);
            if (!string.IsNullOrEmpty(animationName))
            {
                startingAnimation = m_animationName;
                var animation = dataAsset.GetSkeletonInstancingData().FindAnimation(animationName);
                if (animation.IsValid)
                {
                    m_animationSate.SetAnimation(animation, loop);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        UpdateAnimation(0f);
#endif
                }
            }
            valid = true;
        }

        private void Update()
        {
            if (!valid)
            {
                return;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Update(0f);
                return;
            }
#endif
            Update(unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        public void Update(float deltaTime) 
        {
            deltaTime *= timeScale;
            UpdateAnimation(deltaTime);
            ApplyAnimation();
        }

        void UpdateAnimation(float deltaTime) 
        {
            m_animationSate.Update(deltaTime);
        }

        public void ApplyAnimation() 
        {
            m_animationSate.Apply(this);
        }


        internal void GetPropertyBlock(MaterialPropertyBlock block) 
        {
            m_meshRenderer.GetPropertyBlock(block);
        }

        internal void SetPropertyBlock(MaterialPropertyBlock block) 
        {
            m_meshRenderer.SetPropertyBlock(block);
        }
    }
}