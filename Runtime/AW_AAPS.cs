#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDKBase;
using nadena.dev.ndmf;
using ANGELWARE.AW_APS;
using ANGELWARE.AW_AAPS;
using System.Collections.Generic;
using AnimatorAsCode.V1.ModularAvatar;
using AnimatorAsCode.V1.NDMFProcessor;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

[assembly: ExportsPlugin(typeof(AW_AAPSPlugin))]

namespace ANGELWARE.AW_AAPS
{
    public class AW_AAPS : MonoBehaviour, IEditorOnly
    {
        public string oneFloatParameter = "Internal/Float";
        public List<AW_HoleMarkerAnimation> holeMarkers;
    }

    public class AW_AAPSPlugin : AacPlugin<AW_AAPS>
    {
        protected override AacPluginOutput Execute()
        {
            var ctrl = aac.NewAnimatorController();
            var fx = ctrl.NewLayer("AnimatedAPS");
            var maAc = MaAc.Create(my.gameObject);
            
            var apsComponent = Object.FindObjectOfType<AW_Aps>();
            if (apsComponent == null)
            {
                Debug.LogError("An AAPS component is present on the avatar but no APS component " +
                               "could be found! Are you sure you have set up APS? Aborting setup...");
                return AacPluginOutput.Regular();
            }

            if (my.holeMarkers.Count == 0)
            {
                Debug.Log("AAPS Didn't find any hole markers, skipping...");
            }

            var oneFloat = aac.NoAnimator().FloatParameter(my.oneFloatParameter);
            var masterDbt = aac.NewBlendTree().Direct();

            foreach (var marker in my.holeMarkers)
            {
                var entranceParameterString = $"NSFW/Input/{marker.holeMarker.tag}/Entrance";
                var depthParameterString = $"NSFW/Input/{marker.holeMarker.tag}/Max";

                var entranceParmeter = fx.FloatParameter(entranceParameterString);
                var depthParmeter = fx.FloatParameter(depthParameterString);

                var entranceTree = aac.NewBlendTree().Simple1D(entranceParmeter);
                foreach (var entranceAnimation in marker.entranceAnimations)
                {
                    entranceTree.WithAnimation(entranceAnimation.animation, entranceAnimation.threshold);
                }
                
                var depthTree = aac.NewBlendTree().Simple1D(depthParmeter);
                foreach (var depthAnimations in marker.depthAnimations)
                {
                    depthTree.WithAnimation(depthAnimations.animation, depthAnimations.threshold);
                }
                
                masterDbt.WithAnimation(entranceTree, oneFloat);
                masterDbt.WithAnimation(depthTree, oneFloat);
                
                Debug.Log($"AAPS Adding {marker.holeMarker.tag} to tree...");
            }

            fx.NewState("MasterDBT").WithAnimation(masterDbt).WithWriteDefaultsSetTo(true);
            
            maAc.NewMergeAnimator(ctrl, VRCAvatarDescriptor.AnimLayerType.FX);
            
            return AacPluginOutput.Regular();
        }
    }

    [Serializable]
    public class AW_HoleMarkerAnimation
    {
        public AW_ApsHoleMarker holeMarker;
        public AW_Animation[] entranceAnimations;
        public AW_Animation[] depthAnimations;
    }

    [Serializable]
    public class AW_Animation
    {
        public AnimationClip animation;
        public float threshold;
    }
}

#endif