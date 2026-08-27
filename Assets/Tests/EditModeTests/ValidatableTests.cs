using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuantumUser.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Yggdrasill.Tests.EditMode
{
    public readonly struct ValidationIssue
    {
        public readonly UnityEngine.Object Context;
        public readonly string Message;

        public ValidationIssue(UnityEngine.Object context, string message)
        {
            Context = context;
            Message = message;
        }

        public ValidationIssue(UnityEngine.Object context, List<string> messages)
            : this(context, string.Join(Environment.NewLine, messages))
        {
        }

        public override string ToString()
        {
            return $"객체 {Context.name} 에서 검증 실패:" + Environment.NewLine + Message;
        }
    }

    public class ValidatableTests
    {
        private static void ValidateScriptableObjects(List<ValidationIssue> issues)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IValidatable>())
            {
                if (typeof(ScriptableObject).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsGenericTypeDefinition)
                {
                    ValidateScriptableObjectsOfType(type, issues);
                }
            }
        }

        private static void ValidateScriptableObjectsOfType(Type type, List<ValidationIssue> issues)
        {
            foreach (var guid in AssetDatabase.FindAssetGUIDs($"t:{type.Name}"))
            {
                var assetObject = AssetDatabase.LoadAssetByGUID<ScriptableObject>(guid);
                if (assetObject is IValidatable v)
                {
                    var validationResult = v.Validate();
                    if (validationResult.Count != 0)
                    {
                        var validationIssue = new ValidationIssue(assetObject, validationResult);
                        issues.Add(validationIssue);
                    }
                }
            }
        }

        static void CollectFromPrefabs(List<ValidationIssue> issues)
        {
            var guids = AssetDatabase.FindAssetGUIDs("t:Prefab");
            Array.Sort(guids); // FindAssets 순서는 보장되지 않음.

            for (int i = 0; i < guids.Length; i++)
            {
                var prefabObject = AssetDatabase.LoadAssetByGUID<GameObject>(guids[i]);
                if (prefabObject != null)
                {
                    var validatableComponents = prefabObject.GetComponentsInChildren<IValidatable>(true);
                    foreach (var component in validatableComponents)
                    {
                        var validationResult = component.Validate();
                        if (validationResult.Count != 0)
                        {
                            var validationIssue = new ValidationIssue((UnityEngine.Object)component, validationResult);
                            issues.Add(validationIssue);
                        }
                    }
                }

                if (i % 200 == 199)
                    EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        [Test]
        public void AllAssetsAreValid()
        {
            var issues = new List<ValidationIssue>();
            ValidateScriptableObjects(issues);
            CollectFromPrefabs(issues);
            foreach (var issue in issues)
            {
                Debug.Log(issue.Message, issue.Context);
            }

            Assert.IsEmpty(issues, string.Join("\n", issues));
        }
        
        [Test]
        public void AllBuildScenesAreValid()
        {
            var issues = new List<ValidationIssue>();
            foreach (var sceneInfo in SceneList.All)
            {
                // 이미 열려 있으면(사용자가 편집 중) 다시 열지 않고 그대로 사용
                bool openedInEditor = sceneInfo.scene.TryGetLoadedScene(out var openedScene);

                if (!openedInEditor)
                    openedScene = EditorSceneManager.OpenScene(sceneInfo.scene.Path, OpenSceneMode.Additive);

                try
                {
                    foreach (var rootObject in openedScene.GetRootGameObjects())
                    foreach (var v in rootObject.GetComponentsInChildren<IValidatable>(true))
                    {
                        var validationResult =  v.Validate();
                        if (validationResult.Count != 0)
                        {
                            var validationIssue = new ValidationIssue((UnityEngine.Object)v, validationResult);
                            Debug.LogError(validationIssue.Message, validationIssue.Context);
                            issues.Add(validationIssue);
                        }
                    }
                }
                finally
                {
                    if (!openedInEditor)
                        EditorSceneManager.CloseScene(openedScene, removeScene: true);
                }
            }

            Assert.IsEmpty(issues, string.Join("\n", issues));
        }
        
        [TearDown]
        public void TearDown()
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
        }
    }
}