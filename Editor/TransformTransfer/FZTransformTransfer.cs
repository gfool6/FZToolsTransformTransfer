using System.Diagnostics;
using System.Collections.Specialized;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
using EUI = FZTools.EditorUtils.UI;
using ELayout = FZTools.EditorUtils.Layout;
using static FZTools.AvatarUtils;

using nadena.dev.modular_avatar.core;

namespace FZTools
{
    public class FZTransformTransfer : EditorWindow
    {
        [SerializeField] GameObject targetAvatar;
        [SerializeField] GameObject sourceAvatar;

        bool isBasicBonesOnly = false;
        bool isTransferPosition = false;
        bool isTransferRotation = false;
        bool isTransferScale = true;
        bool isTransferScaleAdjustor = true;

        [MenuItem("FZTools/ScaleTransfer(β)")]
        private static void OpenWindow()
        {
            var window = GetWindow<FZTransformTransfer>();
            window.titleContent = new GUIContent("ScaleTransfer(β)");
        }

        private void OnGUI()
        {
            ELayout.Horizontal(() =>
            {
                EUI.Space();
                ELayout.Vertical(() =>
                {
                    var text = "以下の機能を提供します\n"
                            + "・いわゆるロリ化prefabなどのScale数値を既存アバターに適用できます\n"
                            + "・基本機能としてはTransformのScaleとMA Scale Adjustorを対象とします\n"
                            + "・OptionでPosition/Rotationの転送も可能です\n";
                    EUI.InfoBox(text);

                    EUI.Label("Target Avatar");
                    EUI.ChangeCheck(
                        () => EUI.ObjectField<GameObject>(ref targetAvatar),
                        () =>
                        {
                        });
                    EUI.Space();

                    EUI.Label("Source Avatar");
                    EUI.ChangeCheck(
                        () => EUI.ObjectField<GameObject>(ref sourceAvatar),
                        () =>
                        {
                        });
                    EUI.Space();

                    EUI.Label("Option");
                    EUI.Space();
                    // 基礎ボーンのみ対象とTransformそれぞれにチェックボックス
                    EUI.ToggleWithLabel(ref isBasicBonesOnly, "基礎的なHumanoid Boneのみ対象");
                    EUI.ToggleWithLabel(ref isTransferPosition, "Positionを転送");
                    EUI.ToggleWithLabel(ref isTransferRotation, "Rotationを転送");
                    EUI.ToggleWithLabel(ref isTransferScale, "Scaleを転送");
                    EUI.ToggleWithLabel(ref isTransferScaleAdjustor, "MA Scale Adjustorを転送");
                    EUI.Space();

                    EUI.Space(2);
                    EUI.Button("作成", Transfer);
                });
            });
        }

        private void Transfer()
        {
            if (targetAvatar == null || sourceAvatar == null)
            {
                UnityEngine.Debug.LogError("Target AvatarまたはSource Avatarが設定されていません");
                return;
            }

            // Armatureの処理
            var targetArmature = GetArmature(targetAvatar);
            var sourceArmature = GetArmature(sourceAvatar);
            if (targetArmature == null || sourceArmature == null)
            {
                UnityEngine.Debug.LogError("Target AvatarまたはSource AvatarにArmatureが見つかりません");
                return;
            }
            TransferTransform(targetArmature, sourceArmature);
            if (isTransferScaleAdjustor)
            {
                TransferScaleAdjustor(targetArmature, sourceArmature);
            }

            // Armature以下の全Transformの処理
            var targetTransforms = GetTransforms(targetAvatar);
            var sourceTransforms = GetTransforms(sourceAvatar);
            foreach (var st in sourceTransforms)
            {
                var tt = targetTransforms.FirstOrDefault(t => t.name == st.name);
                if (tt != null)
                {
                    TransferTransform(tt.gameObject, st.gameObject);
                    if (isTransferScaleAdjustor)
                    {
                        TransferScaleAdjustor(tt.gameObject, st.gameObject);
                    }
                }
            }

            // 最後に、AvatarのRootのScaleも転送する
            var targetRoot = targetAvatar.transform;
            var sourceRoot = sourceAvatar.transform;
            targetRoot.localScale = sourceRoot.localScale;
        }

        private GameObject GetArmature(GameObject avatar)
        {
            Animator animator = avatar.GetComponent<Animator>();
            return AvatarUtils.GetArmature(animator);
        }

        private Transform[] GetTransforms(GameObject avatar)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (isBasicBonesOnly)
            {
                var basicBones = Enum.GetValues(typeof(HumanBodyBones)).OfType<HumanBodyBones>().Select(bone =>
                {
                    if(bone == HumanBodyBones.LastBone)
                    {
                        return null;
                    }
                    return animator.GetBoneTransform(bone);
                });
                return basicBones.Where(t => t != null).ToArray();
            }
            var armature = animator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject;
            return armature.GetComponentsInChildren<Transform>(); ;
        }

        private void TransferTransform(GameObject target, GameObject source)
        {
            var targetTransforms = target.transform;
            var sourceTransforms = source.transform;

            if (isTransferPosition)
            {
                targetTransforms.localPosition = sourceTransforms.localPosition;
            }
            if (isTransferRotation)
            {
                targetTransforms.localRotation = sourceTransforms.localRotation;
            }
            if (isTransferScale)
            {
                targetTransforms.localScale = sourceTransforms.localScale;
            }
        }

        private void TransferScaleAdjustor(GameObject target, GameObject source)
        {
            var sourceSA = source.GetComponent<ModularAvatarScaleAdjuster>();
            if (sourceSA != null)
            {
                var targetSA = target.GetComponent<ModularAvatarScaleAdjuster>();
                if (targetSA == null)
                {
                    targetSA = target.AddComponent<ModularAvatarScaleAdjuster>();
                }
                targetSA.Scale = sourceSA.Scale;
            }
        }
    }
}