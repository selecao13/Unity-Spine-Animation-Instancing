using UnityEngine;
using Spine.Instancing;
public class Example : MonoBehaviour
{
    public GameObject examplePrefeb;
    public int instanceCount = 100;
    void Start()
    {
        var skeletonInstancingComp = examplePrefeb.GetComponent<SkeletonInstancing>();
        var animations = skeletonInstancingComp.instanceData.animations;
        var posRange = instanceCount / 2f;
        for (int i = 0; i < instanceCount; i++) 
        {
            var randomPos = new Vector3(Random.Range(-posRange, posRange), Random.Range(-posRange, posRange), Random.Range(-posRange, posRange));
            var clone = Instantiate(examplePrefeb, randomPos,Quaternion.identity);
            var cloneInstancingComp = clone.GetComponent<SkeletonInstancing>();
            var cloneAnim = animations[Random.Range(0, animations.Length)];
            cloneInstancingComp.animationSate.SetAnimation(cloneAnim,true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
