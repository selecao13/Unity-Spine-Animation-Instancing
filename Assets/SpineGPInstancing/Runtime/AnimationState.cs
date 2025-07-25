using UnityEngine;
using System;
using System.Collections.Generic;

namespace Spine.Instancing
{
    public struct InstancingAnimatedData
    {
        public int prevFrame;
        public int nextFrame;
        public float frameProgress;
    }
    public class TrackEntry: Pool<TrackEntry>.IPoolable
    {
        public Animation animation;
        public float animationStart;
        public float animationEnd;
        public float timeScale;
        public float trackTime;
        public float mixDuration;
        public float mixTime;
        public bool isComplete;
        public bool isLoop;

        public InstancingAnimatedData GetAnimatedData() 
        {
            InstancingAnimatedData result = new InstancingAnimatedData();
            var totalFrame = Mathf.FloorToInt(trackTime / animation.frameInterval);
            if (!isLoop && totalFrame >= animation.frameCount - 1)
            {
                result.prevFrame = animation.frameCount + animation.frameOffset -1;
                result.frameProgress = 0.0f;
            }
            else
            {
                var localPrevFrame = totalFrame % (animation.frameCount - 1); 
                result.prevFrame = localPrevFrame + animation.frameOffset;
                result.frameProgress = Mathf.Clamp01((trackTime - totalFrame * animation.frameInterval) / animation.frameInterval);
            }
            return result;
        }

        public void Reset()
        {
            animationStart = 0;
            animationEnd = 0;
            timeScale = 1;
            trackTime = 0;
            mixDuration = 0;
            isComplete = false;
            isLoop = false;
        }
    }

    public struct Animation 
    {
        public string name { get; private set; }
        public int fps { get; private set; }
        public int frameOffset { get; private set; }
        public int frameCount { get; private set; }
        public float duration { get; private set; }
        public float frameInterval { get; private set; }

        public Animation(string animationName,int fps,int frameOffset,int frameCount)
        {
            this.name = animationName;
            this.fps = fps;
            this.frameOffset = frameOffset;
            this.frameCount = frameCount;
            frameInterval = 1.0f / fps;
            duration = (frameCount  -1)* frameInterval;
        }

        public bool IsValid => frameCount > 1;
    }

    public class AnimationState
    {
        public float timeScale = 1;

        private TrackEntry m_prevEntry;
        public TrackEntry currentEntry { get; private set; }

        private readonly Pool<TrackEntry> trackEntryPool = new Pool<TrackEntry>();

        private SkeletonInstancingData m_instancingData;

        private MaterialPropertyBlock m_materialBlock;

        public AnimationState(SkeletonInstancingData instancingData) 
        {
            m_instancingData = instancingData;
            m_materialBlock = new MaterialPropertyBlock();
        }

        public TrackEntry SetAnimation(string name,bool loop) 
        {
            var animation = m_instancingData.FindAnimation(name);
            if (animation.IsValid) 
            {
                var trackEnty = NewTrackEntry(in animation, loop);
                if (currentEntry != null) 
                {
                    if (m_prevEntry != null)
                    {
                        trackEntryPool.Free(m_prevEntry);
                    }
                    m_prevEntry = currentEntry;
                }
                currentEntry = trackEnty;
                return trackEnty;
            }
            return null;
        }

        public void SetAnimation(Animation animation, bool loop) 
        {
            if (!animation.IsValid) 
            {
                return;
            }
            SetAnimation(animation.name, loop);
        }

        internal void Update(float delta)
        {
            if (currentEntry != null && !currentEntry.isComplete)
            {
                delta *= timeScale;
                float currentDelta = delta * currentEntry.timeScale;
                currentEntry.trackTime += currentDelta;
                if (currentEntry.trackTime >= currentEntry.animationEnd) 
                {
                    if (currentEntry.isLoop)
                    {
                        currentEntry.trackTime = 0;
                    }
                    else 
                    {
                        currentEntry.trackTime = currentEntry.animationEnd;
                        currentEntry.isComplete = true;
                    }
                }
            }
        }

        public void Apply(SkeletonInstancing skeletonInstancing) 
        {
            if (currentEntry == null) 
            {
                return; 
            }

            var playData = currentEntry.GetAnimatedData();
            m_materialBlock.Clear();
            skeletonInstancing.GetPropertyBlock(m_materialBlock);
            m_materialBlock.SetFloat("FrameIndex", playData.prevFrame);
            m_materialBlock.SetFloat("TransitionProgress", playData.frameProgress);
            skeletonInstancing.SetPropertyBlock(m_materialBlock);
        }

        public TrackEntry GetCurrent() 
        {
            return currentEntry;
        }

        private TrackEntry NewTrackEntry(in Animation animation,bool loop) 
        {
            var entry = trackEntryPool.Obtain();
            entry.animation = animation;
            entry.animationStart = 0;
            entry.animationEnd = animation.duration;
            entry.timeScale = 1;
            entry.isLoop = loop;
            return entry;
        }

        public void ClearTrack() 
        {
            currentEntry = null;
        }
    }

    class Pool<T> where T : class, new()
    {
        public readonly int max;
        readonly Stack<T> freeObjects;

        public int Count { get { return freeObjects.Count; } }
        public int Peak { get; private set; }

        public Pool(int initialCapacity = 16, int max = int.MaxValue)
        {
            freeObjects = new Stack<T>(initialCapacity);
            this.max = max;
        }

        public T Obtain()
        {
            return freeObjects.Count == 0 ? new T() : freeObjects.Pop();
        }

        public void Free(T obj)
        {
            if (obj == null) throw new ArgumentNullException("obj", "obj cannot be null");
            if (freeObjects.Count < max)
            {
                freeObjects.Push(obj);
                Peak = Math.Max(Peak, freeObjects.Count);
            }
            Reset(obj);
        }

        public void Clear()
        {
            freeObjects.Clear();
        }

        protected void Reset(T obj)
        {
            IPoolable poolable = obj as IPoolable;
            if (poolable != null) poolable.Reset();
        }

        public interface IPoolable
        {
            void Reset();
        }
    }
}
