#if UNITY_EDITOR
using ANGELWARE.AW_AAPS;
using ANGELWARE.AW_APS;
using AnimatorAsCode.V1;
using AnimatorAsCode.V1.ModularAvatar;
using AnimatorAsCode.V1.NDMFProcessor;
using AnimatorAsCode.V1.VRC;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(AW_AAPSPenetrator))]

namespace ANGELWARE.AW_AAPS
{
    public class AW_AAPSPenetrator : AacPlugin<AW_ApsPenetratorComponent>
    {
        protected override AacPluginOutput Execute()
        {
            var ctrl = aac.NewAnimatorController("Test" + "_" + my.parameterName);
            var fx = ctrl.NewLayer("FX");
            var pDepth = fx.FloatParameter($"APS/{my.parameterName}/Depth");
            var pWetness = fx.FloatParameter($"APS/{my.parameterName}/Wetness");
            var pMultipliedTime = fx.FloatParameter(my.smoothingMultiplierParam);
            var pOneFloat = fx.FloatParameter(my.oneFloatParameter);

            var smoothedAnimationTree =
                SmoothedToggle(pDepth, pWetness, pOneFloat, pMultipliedTime, my.wetnessAnimations);
            fx.NewState("SmoothedAnims").WithAnimation(smoothedAnimationTree).WithWriteDefaultsSetTo(true);

            var idleLayer = ctrl.NewLayer("Idle");
            FuckingKillMe(idleLayer);
            
            //
            // // Create early so we can generate parameters easily for other layers.
            // var dbtMasterLayer = ctrl.NewLayer("DBTMaster");
            //
            // #region Parameters
            //
            // var pOneFloat = dbtMasterLayer.FloatParameter("Internal/Float");
            // var pFrameTime = dbtMasterLayer.FloatParameter("Internal/Time");
            //
            // var pWetness = dbtMasterLayer.FloatParameter($"APS/{my.parameterName}/Wetness");
            // var pDepth = dbtMasterLayer.FloatParameter($"APS/{my.parameterName}/Depth");
            // var pDecay = dbtMasterLayer.FloatParameter($"APS/{my.parameterName}/Decay");
            // var pNew = dbtMasterLayer.BoolParameter($"APS/{my.parameterName}/New");
            // var pDrying = dbtMasterLayer.BoolParameter($"APS/{my.parameterName}/Drying");
            // var pReset = dbtMasterLayer.BoolParameter($"APS/{my.parameterName}/Reset");
            //
            // #endregion
            //
            // #region Layers
            //
            // var dbtMaster = DBTMaster(pWetness);
            // dbtMasterLayer.NewState("(WD On) MasterDBT").WithWriteDefaultsSetTo(true).WithAnimation(dbtMaster);
            //
            // var depthLayer = ctrl.NewLayer("Depth");
            // var depth = DepthMaster(pOneFloat, pWetness, pDepth);
            // depthLayer.NewState("(WD On) MasterDBT").WithWriteDefaultsSetTo(true).WithAnimation(depth);
            //
            // var wetnessLayer = ctrl.NewLayer("Wetness");
            // GenerateWetnessLayer(wetnessLayer, pWetness, pOneFloat, pDepth, pNew);
            //
            // var dryingLayer = ctrl.NewLayer("Drying");
            // GenerateDryingLayer(dryingLayer, pNew, pDrying);
            //
            // var decayLayer = ctrl.NewLayer("Decay");
            // GenerateDecayLayer(decayLayer, pWetness, pDecay, pFrameTime, pDrying);
            //
            // var resetLayer = ctrl.NewLayer("Reset");
            // GenerateResetLayer(resetLayer, pWetness, pReset);
            //
            // var animationLayer = ctrl.NewLayer("Animations");
            // GenerateAnimationLayer(animationLayer, pOneFloat, pWetness, my.wetnessAnimations);
            //
            // #endregion
            //
            var maAc = MaAc.Create(my.gameObject);
            maAc.NewMergeAnimator(ctrl, VRCAvatarDescriptor.AnimLayerType.FX);

            return AacPluginOutput.Regular();
        }

        #region WD On Style
        private AacFlBlendTreeDirect DBTMaster(
            AacFlFloatParameter pWetness
        )
        {
            var masterDBT = aac.NewBlendTree().Direct();

            #region Hold Value

            var wetnessHoldClip = aac.NewClip("HoldWetness").Animating(clip =>
            {
                clip.AnimatesAnimator(pWetness).WithOneFrame(1.0f);
            });

            masterDBT.WithAnimation(wetnessHoldClip, pWetness);

            #endregion

            return masterDBT;
        }

        private AacFlBlendTreeDirect DepthMaster(
            AacFlFloatParameter pOneFloat,
            AacFlFloatParameter pWetness,
            AacFlFloatParameter pDepth
        )
        {
            var masterDBT = aac.NewBlendTree().Direct();

            #region Depth calc

            var depthPositiveClip = aac.NewClip("DepthPositive").Animating(clip =>
            {
                clip.AnimatesAnimator(pDepth).WithOneFrame(1.0f);
            });

            var depthNegativeClip = aac.NewClip("DepthPositive").Animating(clip =>
            {
                clip.AnimatesAnimator(pDepth).WithOneFrame(-1.0f);
            });

            var depthTree = aac.NewBlendTree().Direct().WithAnimation(depthPositiveClip, pWetness)
                .WithAnimation(depthNegativeClip, pDepth);

            masterDBT.WithAnimation(depthTree, pOneFloat);

            #endregion


            return masterDBT;
        }

        private void GenerateDryingLayer(
            AacFlLayer dryingLayer,
            AacFlBoolParameter pNew,
            AacFlBoolParameter pDrying
        )
        {
            var dummyClip = aac.DummyClipLasting(1, AacFlUnit.Frames);
            var dummyClip3 = aac.DummyClipLasting(3, AacFlUnit.Seconds);

            var initState = dryingLayer.NewState("Init").WithAnimation(dummyClip).WithWriteDefaultsSetTo(false);
            var wetState = dryingLayer.NewState("Wet").WithAnimation(dummyClip3).WithWriteDefaultsSetTo(false);
            var dryState = dryingLayer.NewState("Dry").WithAnimation(dummyClip).WithWriteDefaultsSetTo(false);

            initState.TransitionsTo(wetState).When(pNew.IsTrue());
            wetState.TransitionsTo(initState).When(pNew.IsTrue());
            wetState.TransitionsTo(dryState).AfterAnimationFinishes();

            initState.Drives(pDrying, false);
            wetState.Drives(pNew, false);
            dryState.Drives(pDrying, true);

            dryState.Exits().When(pNew.IsTrue());
        }

        private void GenerateWetnessLayer(
            AacFlLayer wetnessLayer,
            AacFlFloatParameter pWetness,
            AacFlFloatParameter pOneFloat,
            AacFlFloatParameter pDepth,
            AacFlBoolParameter pNew
        )
        {
            var wetnessDbt = aac.NewBlendTree().Direct();

            #region Set Wetness

            var wetnessSetClip = aac.NewClip("SetWetness").Animating(clip =>
            {
                clip.AnimatesAnimator(pWetness).WithOneFrame(1.0f);
            });

            var tree = aac.NewBlendTree().Direct().WithAnimation(wetnessSetClip, pDepth);

            wetnessDbt.WithAnimation(tree, pOneFloat);

            #endregion

            var idleState = wetnessLayer.NewState("Init").WithAnimation(aac.DummyClipLasting(1, AacFlUnit.Frames));
            var wetnessState = wetnessLayer.NewState("Set Wetness").WithAnimation(wetnessDbt);

            idleState.TransitionsTo(wetnessState).When(pDepth.IsLessThan(0));
            wetnessState.Exits().Automatically();

            wetnessState.Drives(pNew, true);
        }

        private void GenerateDecayLayer(
            AacFlLayer decayLayer,
            AacFlFloatParameter pWetness,
            AacFlFloatParameter pDecay,
            AacFlFloatParameter pFrametime,
            AacFlBoolParameter pDrying
        )
        {
            var idleState = decayLayer.NewState("Init").WithAnimation(aac.DummyClipLasting(1, AacFlUnit.Frames));

            #region DBT

            var decayTree = aac.NewBlendTree().Direct();

            var wetnessPositiveClip = aac.NewClip("SetWetness").Animating(clip =>
            {
                clip.AnimatesAnimator(pWetness).WithOneFrame(1.0f);
            });

            var wetnessNegativeClip = aac.NewClip("SetWetnessNeg").Animating(clip =>
            {
                clip.AnimatesAnimator(pWetness).WithOneFrame(-1.0f);
            });

            var frameTimeTree = aac.NewBlendTree().Direct().WithAnimation(wetnessNegativeClip, pFrametime);

            decayTree.WithAnimation(wetnessPositiveClip, pWetness);
            decayTree.WithAnimation(frameTimeTree, pDecay);

            #endregion

            var treeState = decayLayer.NewState("Decay").WithAnimation(decayTree);

            idleState.TransitionsTo(treeState).When(pDrying.IsTrue());
            treeState.Exits().When(pDrying.IsFalse());
        }

        private void GenerateResetLayer(
            AacFlLayer resetLayer,
            AacFlFloatParameter pWetness,
            AacFlBoolParameter pReset
        )
        {
            var idleState = resetLayer.NewState("Init").WithAnimation(aac.DummyClipLasting(1, AacFlUnit.Frames));

            var resetState = resetLayer.NewState("Reset").WithAnimation(aac.NewClip("Reset").Animating(clip =>
            {
                clip.AnimatesAnimator(pWetness).WithOneFrame(0f);
            }));

            idleState.TransitionsTo(resetState).When(pReset.IsTrue());
            resetState.Exits().When(pReset.IsFalse());
        }

        private void GenerateAnimationLayer(
            AacFlLayer animationLayer,
            AacFlFloatParameter pOneFloat,
            AacFlFloatParameter pWetness,
            AW_ApsSerializableAnimations[] animations
        )
        {
            var tree = aac.NewBlendTree().Direct();
            var animationTree = aac.NewBlendTree().Simple1D(pWetness);

            foreach (var animation in animations) animationTree.WithAnimation(animation.animation, animation.trigger);

            tree.WithAnimation(animationTree, pOneFloat);

            animationLayer.NewState("AnimationTree").WithAnimation(tree).WithWriteDefaultsSetTo(true);
        }
        #endregion

        public AacFlBlendTreeDirect SmoothedToggle(
            AacFlFloatParameter pInput, 
            AacFlFloatParameter pOutput, 
            AacFlFloatParameter pOneFloat, 
            AacFlFloatParameter pMultipliedTime, 
            AW_ApsSerializableAnimations[] animations
            )
        {
            var animation1d = aac.NewBlendTree().Simple1D(pOutput);

            foreach (var animation in animations)
            {
                animation1d.WithAnimation(animation.animation, animation.trigger);
            }
            
            var aap0 = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(pOutput).WithOneFrame(0.0f);
                }
            );
            
            var aap1 = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(pOutput).WithOneFrame(1.0f);
                }
            );
            
            var smoothed1d = aac.NewBlendTree()
                .Simple1D(pOutput)
                .WithAnimation(aap0, 0.0f)
                .WithAnimation(aap1, 1.0f);

            var menu1d = aac.NewBlendTree()
                .Simple1D(pInput)
                .WithAnimation(aap0, 0.0f)
                .WithAnimation(aap1, 1.0f);

            var smoothing1d = aac.NewBlendTree()
                .Simple1D(pMultipliedTime)
                .WithAnimation(smoothed1d, 0.0f)
                .WithAnimation(menu1d, 1.0f);

            var dbt = aac.NewBlendTree().Direct()
                .WithAnimation(smoothing1d, pOneFloat)
                .WithAnimation(animation1d, pOneFloat);

            return dbt;
        }

        public void FuckingKillMe(AacFlLayer idleLayer)
        {
            idleLayer.NewState("PenIdleAnimation").WithAnimation(my.idleMovementAnimation).WithSpeedSetTo(0.25f);
        }
    }
}
#endif