#if UNITY_EDITOR
using ANGELWARE.AW_AAPS;
using ANGELWARE.AW_APS;
using AnimatorAsCode.V1;
using AnimatorAsCode.V1.ModularAvatar;
using AnimatorAsCode.V1.NDMFProcessor;
using AnimatorAsCode.V1.VRC;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(AW_AAPSPenetrator))]

namespace ANGELWARE.AW_AAPS
{
    public class AW_AAPSPenetrator : AacPlugin<AW_ApsPenetratorComponent>
    {
        protected override AacPluginOutput Execute()
        {
            #region Setup
            var ctrl = aac.NewAnimatorController("Penetrator" + "_" + my.parameterName);
            var fx = ctrl.NewLayer("FX");
            var maAc = MaAc.Create(my.gameObject);
            #endregion
            
            #region Parameters
            var pDepth = fx.FloatParameter($"APS/{my.parameterName}/Depth");
            var pWetness = fx.FloatParameter($"APS/{my.parameterName}/Wetness");
            var pWetnessIOD = fx.FloatParameter($"APS/{my.parameterName}/Wetness/IOD");
            var pSkinPos = fx.FloatParameter($"APS/{my.parameterName}/SkinPos");

            var pFrotEnable = fx.FloatParameter($"APS/{my.parameterName}/Frot/Enable");

            var pInputErection = fx.FloatParameter($"APS/{my.parameterName}/Erection/Input");
            var pOutputErection = fx.FloatParameter($"APS/{my.parameterName}/Erection/Output");
            var pTouchErection = fx.FloatParameter($"APS/{my.parameterName}/Erection/Touch");
            var pSmoothErection = fx.FloatParameter($"APS/{my.parameterName}/Erection/Smooth");
            var pIODErection = fx.FloatParameter($"APS/{my.parameterName}/Erection/IOD");
            var pPhysGrabErection = fx.FloatParameter($"{my.parameterName}_IsGrabbed");
            
            var pInputShaftSize = fx.FloatParameter($"APS/{my.parameterName}/Size/Shaft");
            var pInputBallsSize = fx.FloatParameter($"APS/{my.parameterName}/Size/Balls");
            var pInputShaftAppearance = fx.FloatParameter($"APS/{my.parameterName}/Appearance/Shaft");
            var pInputBallsAppearance = fx.FloatParameter($"APS/{my.parameterName}/Appearance/Balls");
            // Menu Parameter <- From the menu
            var pInputForeskinLength = fx.FloatParameter($"APS/{my.parameterName}/Appearance/Foreskin");
            // Driver Parameter <- Driven via animation clip to drive actual animation
            var pDriverForeskinLength = fx.FloatParameter($"APS/{my.parameterName}/Appearance/Foreskin/Driver");
            var pForeskinLengthMult = fx.FloatParameter($"APS/{my.parameterName}/Appearance/Foreskin/Multiplier");
            fx.OverrideValue(pForeskinLengthMult, 1.0f);
            var pForeskinLengthInv = fx.FloatParameter($"APS/{my.parameterName}/Appearance/Foreskin/Inv");
            var pForeskinIOD = fx.FloatParameter($"APS/{my.parameterName}/Appearance/Foreskin/IOD");

            var pMultipliedTime = fx.FloatParameter(my.smoothingMultiplierParam);
            var pOneFloat = fx.FloatParameter(my.oneFloatParameter);
            
            var pMultipliedWetnessTime = fx.FloatParameter($"APS/{my.parameterName}/WetnessSpeed");
            var pMultipliedSkinTime = fx.FloatParameter($"APS/{my.parameterName}/SkinSpeed");
            
            var wetnessSpeed = my.wetnessSpeed;
            var skinSpeed = my.skinMovementSpeed;
            #endregion

            // Main DBT
            var dbt = aac.NewBlendTree().Direct();
            
            // Frot Enable (Using animation clips to avoid VRCFury dependency)
            if (my.frotEnable != null && my.frotDisable != null)
            {
                // Animation Clips
                var frotEnable = my.frotEnable;
                var frotDisable = my.frotDisable;

                // Toggle Tree
                var tree = aac.NewBlendTree().Simple1D(pFrotEnable).WithAnimation(frotDisable, 0f)
                    .WithAnimation(frotEnable, 1f);

                // Add to DBT
                dbt.WithAnimation(tree, pOneFloat);

                // Add bool control parameter for menu
                maAc.NewBoolToFloatParameter(pFrotEnable);
            }
            
            // Erection
            if (my.erectionAnimations.Length > 0)
            {
                // Erection Animation Tree
                var eTree = SmoothedToggle(pInputErection, pOutputErection, pOneFloat, pMultipliedTime,
                    my.erectionAnimations);
                dbt.WithAnimation(eTree, pOneFloat);
                maAc.NewParameter(pInputErection);

                var pTouchErectionArray = new[]
                {
                    $"APS/{my.parameterName}/Erection/Touch",
                    $"{my.parameterName}_IsGrabbed"
                };
                
                ErectionController("Erection", ctrl, 1.0f, 0.01f,
                    $"APS/{my.parameterName}/Erection/Input",
                    pTouchErectionArray, 10f,
                    0.5f, 0.5f,
                    0.05f, 0.005f);

                
            }
            
            // Wetness
            if (my.wetnessAnimations.Length > 0)
            {
                var multiplierTree = TimeMultiplier(pMultipliedTime, pMultipliedWetnessTime, pOneFloat, wetnessSpeed);
                // var smoothedAnimationTree = SmoothedToggle(pDepth, pWetness, pOneFloat, pMultipliedWetnessTime,
                //     my.wetnessAnimations);
                
                var smoothedAnimationTree = BuildLinearAnimationTree(pDepth, pWetness, pWetnessIOD, pOneFloat, pMultipliedWetnessTime, my.wetnessAnimations);
                
                dbt.WithAnimation(smoothedAnimationTree, pOneFloat);
                dbt.WithAnimation(multiplierTree, pOneFloat);
            }
            
            // Skin Movement FX
            if (my.skinMovementAnimations.Length > 0)
            {
                var skinMultiplierTree = TimeMultiplier(pMultipliedTime, pMultipliedSkinTime, pOneFloat, skinSpeed);
                var skinSlidingTree = SmoothedToggle(pDepth, pSkinPos, pOneFloat, pMultipliedSkinTime,
                    my.skinMovementAnimations);
                
                dbt.WithAnimation(skinMultiplierTree, pOneFloat);
                dbt.WithAnimation(skinSlidingTree, pOneFloat);
            }
            
            // Dick Size
            if (my.sizeAnimations.Length > 0)
            {
                var sizeTree = Slider(pInputShaftSize, my.sizeAnimations);
                dbt.WithAnimation(sizeTree, pOneFloat);
                maAc.NewParameter(pInputShaftSize);
            }
            
            // Balls Size
            if (my.ballsSizeAnimations.Length > 0)
            {
                var ballsSizeTree = Slider(pInputBallsSize, my.ballsSizeAnimations);
                dbt.WithAnimation(ballsSizeTree, pOneFloat);
                maAc.NewParameter(pInputBallsSize);
            }
            
            // Foreskin Amount
            if (my.foreskinAnimations != null)
            {
                var foreskinLengthTree = Slider(pDriverForeskinLength, my.foreskinAnimations);
                dbt.WithAnimation(foreskinLengthTree, pOneFloat);

                var driverAnimation = aac.NewClip("Driver").Animating(clip =>
                {
                    clip.AnimatesAnimator(pDriverForeskinLength).WithOneFrame(1.0f);
                });
                
                var driverTree = aac.NewBlendTree().Direct().WithAnimation(driverAnimation, pInputForeskinLength);

                var multiplierTree = aac.NewBlendTree().Direct().WithAnimation(driverTree, pForeskinLengthMult);
                
                dbt.WithAnimation(multiplierTree, pOneFloat);

                var foreskinOneMinusClip = aac.NewClip("FSOneMinus").Animating(clip =>
                {
                    clip.AnimatesAnimator(pForeskinLengthMult).WithOneFrame(1.0f);
                });
                
                var foreskinOneMinus = new AW_ApsSerializableAnimations()
                {
                    animation = foreskinOneMinusClip.Clip,
                    trigger = 0.0f
                };
                
                var foreskinZeroClip = aac.NewClip("FSZero").Animating(clip =>
                {
                    clip.AnimatesAnimator(pForeskinLengthMult).WithOneFrame(0.0f);
                });
                
                var foreskinZero = new AW_ApsSerializableAnimations()
                {
                    animation = foreskinZeroClip.Clip,
                    trigger = 1.0f
                };
                
                var animArray = new AW_ApsSerializableAnimations[]
                {
                    foreskinOneMinus,
                    foreskinZero
                };
                
                var foreskinMultiplierSmoothed = BuildLinearAnimationTree(pDepth, pForeskinLengthInv, pForeskinIOD, pOneFloat, pMultipliedTime, animArray);
                dbt.WithAnimation(foreskinMultiplierSmoothed, pOneFloat);
                
                maAc.NewParameter(pInputForeskinLength);
            }
            
            // Shaft Appearance
            if (my.shaftAppearanceAnimations.Length > 0)
            {
                var shaftAppearanceTree = Slider(pInputShaftAppearance, my.shaftAppearanceAnimations);
                dbt.WithAnimation(shaftAppearanceTree, pOneFloat);
                maAc.NewParameter(pInputShaftAppearance);
            }
            
            // Balls Appearance
            if (my.ballsAppearanceAnimations.Length > 0)
            {
                var ballsAppearanceTree = Slider(pInputBallsAppearance, my.ballsAppearanceAnimations);
                dbt.WithAnimation(ballsAppearanceTree, pOneFloat);
                maAc.NewParameter(pInputBallsAppearance);
            }
            
            fx.NewState("SmoothedAnims").WithAnimation(dbt).WithWriteDefaultsSetTo(true);
            
            // Cum Stuff, keeping all of this down here out of the way.
            if (my.enableCumEffects)
            {
                // Parameters
                var pPenArousal = fx.FloatParameter("NSFW/Smooth/PenArousal");
                var pGestureLeft = fx.IntParameter("GestureLeft");
                var pGestureRight = fx.IntParameter("GestureRight");
                
                // Layer
                var cumLayer = ctrl.NewLayer("Cum");
                
                // Clip creation
                var defaultsClip = aac.NewClip("Defaults").Animating(clip =>
                {
                    clip.Animates(my.cumObject).WithOneFrame(0.0f);
                    clip.Animates(my.staticCumObject).WithOneFrame(0.0f);
                    clip.Animates(my.ocsFinishSender).WithOneFrame(0.0f);
                });
                var cumClip = aac.NewClip("Cum").Animating(clip =>
                {
                    clip.Animates(my.cumObject).WithFixedSeconds(10, 1.0f);
                    clip.Animates(my.staticCumObject).WithFixedSeconds(10, 0.0f);
                    clip.Animates(my.ocsFinishSender).WithFixedSeconds(10, 1.0f);

                });
                var staticClip = aac.NewClip("Static").Animating(clip =>
                {
                    clip.Animates(my.cumObject).WithFixedSeconds(20, 0.0f);
                    clip.Animates(my.staticCumObject).WithFixedSeconds(20, 1.0f);
                    clip.Animates(my.ocsFinishSender).WithFixedSeconds(20, 0.0f);
                });
                
                // State creation
                var idleState = cumLayer.NewState("Idle").WithAnimation(defaultsClip);
                var cumState = cumLayer.NewState("CumAnimation").WithAnimation(cumClip);
                var staticState = cumLayer.NewState("StaticWaitingCum").WithAnimation(staticClip);
                
                // Hooking up states
                idleState.TransitionsTo(cumState).When(pPenArousal.IsGreaterThan(0.9f)).And(pGestureLeft.IsEqualTo(4));
                idleState.TransitionsTo(cumState).When(pPenArousal.IsGreaterThan(0.9f)).And(pGestureRight.IsEqualTo(4));

                cumState.TransitionsTo(staticState).AfterAnimationFinishes();

                staticState.TransitionsTo(idleState).AfterAnimationFinishes();
            }

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
        
        private void ErectionController(string layerName, AacFlController ctrl, float maxValue, float minValue,
            string inputParameter, string[] desiredParameters, float waitSeconds, float activationThreshold,
            float deactivationThreshold, float increaseBy, float decreaseBy)
        {
            var arousalLayer = ctrl.NewLayer(layerName);
            var nullAnim = aac.DummyClipLasting(1, AacFlUnit.Frames);

            var tenSeconds = aac.DummyClipLasting(10, AacFlUnit.Seconds);

            var twoSeconds = aac.DummyClipLasting(2, AacFlUnit.Seconds);

            var halfSecond = aac.DummyClipLasting(2, AacFlUnit.Seconds);

            var pArousal = arousalLayer.FloatParameter(inputParameter);

            // Idle State
            var idleState = arousalLayer.NewState("Idle").WithAnimation(nullAnim);

            // Init Touch State (From any, player just touched)
            var initTouchState = arousalLayer.NewState("InitTouch").WithAnimation(nullAnim);

            // Add State (From Init, player still touching)
            var addState = arousalLayer.NewState("AddState").WithAnimation(halfSecond);
            addState.DrivingIncreases(pArousal, increaseBy);

            // 10s timer waiting state
            var waitingState = arousalLayer.NewState("Wait").WithAnimation(tenSeconds);

            // Subtraction Buffer State
            var subtractWaitState = arousalLayer.NewState("SubtractWait").WithAnimation(twoSeconds);

            // Subtraction State
            var subtractState = arousalLayer.NewState("Subtract").WithAnimation(nullAnim);
            subtractState.DrivingDecreases(pArousal, decreaseBy);

            // idleState.TransitionsFromEntry(); (Automatic)

            foreach (var desiredParameter in desiredParameters)
            {

                var pOcsTouch = arousalLayer.FloatParameter(desiredParameter);

                initTouchState.TransitionsFromAny().WithNoTransitionToSelf().Automatically()
                    .When(pOcsTouch.IsGreaterThan(activationThreshold));
                initTouchState.TransitionsTo(addState).When(pOcsTouch.IsGreaterThan(activationThreshold))
                    .And(pArousal.IsLessThan(maxValue));
                initTouchState.TransitionsTo(idleState).When(pOcsTouch.IsLessThan(deactivationThreshold))
                    .And(pArousal.IsLessThan(minValue));
                initTouchState.TransitionsTo(waitingState).When(pOcsTouch.IsLessThan(deactivationThreshold))
                    .And(pArousal.IsGreaterThan(minValue));
                
                waitingState.TransitionsTo(addState).When(pOcsTouch.IsGreaterThan(activationThreshold));
                
                subtractWaitState.TransitionsTo(addState).When(pOcsTouch.IsGreaterThan(activationThreshold));
            }

            addState.TransitionsTo(initTouchState).Automatically().WithTransitionDurationSeconds(1f);

            waitingState.TransitionsTo(idleState).When(pArousal.IsLessThan(minValue));
            waitingState.TransitionsTo(subtractWaitState).Automatically().AfterAnimationFinishes();

            subtractWaitState.TransitionsTo(idleState).When(pArousal.IsLessThan(minValue));
            subtractWaitState.TransitionsTo(subtractState).AfterAnimationFinishes();
            subtractState.TransitionsTo(subtractWaitState).AfterAnimationFinishes();
        }

        public AacFlBlendTreeDirect TimeMultiplier(AacFlFloatParameter pInput, AacFlFloatParameter pOutput, AacFlFloatParameter pOneFloat, float speedValue)
        {
            var dbt = aac.NewBlendTree().Direct();
            var multiplier = aac.NewBlendTree().Direct();
            var outputClip = aac.NewClip("SpeedOutput").Animating(clip =>
            {
                clip.AnimatesAnimator(pOutput).WithOneFrame(speedValue);
            });

            multiplier.WithAnimation(outputClip, pInput);
            dbt.WithAnimation(multiplier, pOneFloat);

            return dbt;
        }

        public void FuckingKillMe(AacFlLayer idleLayer)
        {
            idleLayer.NewState("PenIdleAnimation").WithAnimation(my.idleMovementAnimation).WithSpeedSetTo(0.25f);
        }

        public AacFlBlendTree1D Slider(AacFlFloatParameter parameter, AW_ApsSerializableAnimations[] anims)
        {
            var tree = aac.NewBlendTree().Simple1D(parameter);
            foreach (var animations in anims)
            {
                tree.WithAnimation(animations.animation, animations.trigger);
            }

            return tree;
        }
        
        private AacFlBlendTreeDirect BuildLinearAnimationTree(AacFlFloatParameter pInput, AacFlFloatParameter pOutput, AacFlFloatParameter pIODelta, AacFlFloatParameter pOneFloat, AacFlFloatParameter pMultipliedTime, AW_ApsSerializableAnimations[] animations)
        {
            // var floatParam = aac.NoAnimator().FloatParameter(my.oneFloatParameter);
            // var inputParam = aac.NoAnimator().FloatParameter("NSFW/Input/" + parameter);
            // var inputOutputDeltaParam = aac.NoAnimator().FloatParameter("NSFW/InputOutputDelta/" + parameter);
            // var outputParam = aac.NoAnimator().FloatParameter("NSFW/Smooth/" + parameter);

            var floatParam = pOneFloat;
            var inputParam = pInput;
            var inputOutputDeltaParam = pIODelta;
            var outputParam = pOutput;
            var stepSizeParam = pMultipliedTime;
            
            // var stepSizeParam = aac.NoAnimator().FloatParameter(my.smoothingMultiplierParam);
            
            var dbtMaster = aac.NewBlendTree().Direct();
            
            #region I/O Clip Setup
            // Deviating from the original template, these values are modified to drive between 0f to 1f.
            // This is so we don't need to do any value remapping, and it ends up making more sense.
            var aapIODeltaMinus = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(inputOutputDeltaParam).WithOneFrame(-1.0f);
                }
            );
            
            var aapIODeltaPlus = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(inputOutputDeltaParam).WithOneFrame(1.0f);
                }
            );
            
            var aapOutputMinus = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(outputParam).WithOneFrame(0.0f);
                }
            );
            
            var aapOutputPlus = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(outputParam).WithOneFrame(1.0f);
                }
            );
            
            var aapOutputOneMinus = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(outputParam).WithOneFrame(-1.0f);
                }
            );
            
            var aapOutputZero = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(outputParam).WithOneFrame(0.0f);
                }
            );
            
            var aapOutputOnePlus = aac.NewClip().NonLooping().Animating(clip =>
                {
                    clip.AnimatesAnimator(outputParam).WithOneFrame(1.0f);
                }
            );
            #endregion

            #region Logic Tree
            /*
             * Using this setup we are able to achieve a smoothed set of toggles between 0.0f to 1.0f.
             * We are following the equation:
             * 
             * Delta = Value - Smoothed Value
             * SmoothedValue = Value * (1-SmoothAmount) + SmoothedValue * SmoothAmount
             * (Delta)(SmoothedValue)
             *
             * I think this is correct, I suck at math...
             */
            var deltaEqualsInput = aac.NewBlendTree()
                .Simple1D(inputParam)
                .WithAnimation(aapIODeltaMinus, 0f)
                .WithAnimation(aapIODeltaPlus, 1.0f);

            var deltaEqualsNegativeOutput = aac.NewBlendTree()
                .Simple1D(outputParam)
                .WithAnimation(aapIODeltaPlus, 0f)
                .WithAnimation(aapIODeltaMinus, 1.0f);

            var outputEqualsOutput = aac.NewBlendTree()
                .Simple1D(outputParam)
                .WithAnimation(aapOutputMinus, 0f)
                .WithAnimation(aapOutputPlus, 1f);

            var linearBlend = aac.NewBlendTree()
                .Simple1D(inputOutputDeltaParam)
                .WithAnimation(aapOutputOneMinus, -0.1f)
                .WithAnimation(aapOutputZero, 0.0f)
                .WithAnimation(aapOutputOnePlus, 0.1f);

            var multiplierDbt = aac.NewBlendTree().Direct()
                .WithAnimation(deltaEqualsInput, floatParam)
                .WithAnimation(deltaEqualsNegativeOutput, floatParam)
                .WithAnimation(outputEqualsOutput, floatParam)
                .WithAnimation(linearBlend, stepSizeParam);
            
            #endregion
            
            var animationTree = aac.NewBlendTree()
                .Simple1D(outputParam);

            foreach (var animation in animations)
            {
                animationTree.WithAnimation(animation.animation, animation.trigger);
            }
            
            dbtMaster
                .WithAnimation(multiplierDbt, floatParam)
                .WithAnimation(animationTree, floatParam);

            return dbtMaster;
        }
    }
}
#endif