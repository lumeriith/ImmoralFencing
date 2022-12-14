using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ModelDataCharacterVisualizer : ModelComponentBase
{
    [Serializable]
    public class BoneItem
    {
        [HorizontalGroup("A"), HideLabel]
        public string name;
        [HorizontalGroup("A"), HideLabel]
        public HumanBodyBones bone;
        public Quaternion baseRot;
    }

    [ListDrawerSettings()]
    public List<BoneItem> skeletonSettings;
    public Animator connectedCharacter;
    public bool enableRetargeting;
    public bool visualizeIntermediateNode;
    public float bodyScale = 0.1f;
    public float translationScale = 0.01f;
    public Vector3 worldOffset;
    public bool setWorldOffsetPeriodically;
    public float setWorldOffsetInterval = 1f;
    public float hipsYOffset;
    public bool useRootReposition;
    
    private float _lastWorldOffsetTime;
    
    public struct BasicTransform
    {
        public Quaternion rotation;
        public Vector3 position;
    }
    
    private Dictionary<string, BasicTransform> _worldSpaceBoneTransforms = new Dictionary<string, BasicTransform>();

    private Vector3 _prevRootPosition;

    [Button]
    private void BuildUsingCurrentPos()
    {
        for (int i = 0; i < skeletonSettings.Count; i++)
        {
            var item = skeletonSettings[i];
            var characterBone = connectedCharacter.GetBoneTransform(item.bone);
            var modelTransform = _worldSpaceBoneTransforms[item.name];
            var fromRotation = modelTransform.rotation;
            var toRotation = characterBone.rotation;
            
            item.baseRot = Quaternion.Inverse(fromRotation) * toRotation;
        }
    }



    protected override void OnDataChanged()
    {
        base.OnDataChanged();
        
        UpdateBoneTransform(setup.root, new BasicTransform{position = Vector3.zero, rotation = Quaternion.identity});

        if (visualizeIntermediateNode)
        {
            void DrawVisualizationVector(Vector3 end, BasicTransform t, Color color)
            {
                Debug.DrawLine(t.position, t.position + t.rotation * end * 0.15f, color, 1 / 30f);
            }
            
            foreach (var pair in _worldSpaceBoneTransforms)
            {
                var t = pair.Value;
                DrawVisualizationVector(Vector3.right, t, Color.red);
                DrawVisualizationVector(Vector3.forward, t, Color.blue);
                DrawVisualizationVector(Vector3.up, t, Color.green);
            }
        }
        
        if (enableRetargeting)
        {
            foreach (var pair in skeletonSettings)
            {
                var boneTransform = connectedCharacter.GetBoneTransform(pair.bone);
                if (boneTransform == null) continue;

                if (_worldSpaceBoneTransforms.TryGetValue(pair.name, out var tr))
                {
                    if (pair.name == "Hips")
                    {
                        if (useRootReposition)
                        {
                            var xzPos = new Vector3(tr.position.x, 0, tr.position.z);
                            var diff = xzPos - _prevRootPosition;
                            boneTransform.position = tr.position + Vector3.up * hipsYOffset;
                            connectedCharacter.transform.position += diff;
                            _prevRootPosition = xzPos;
                        }
                        else
                        {
                            boneTransform.position = tr.position + Vector3.up * hipsYOffset;
                        }
                    }

                    boneTransform.rotation = tr.rotation;
                    boneTransform.rotation = boneTransform.rotation * pair.baseRot;
                }
            }
        }
    }

    private void UpdateBoneTransform(Joint joint, BasicTransform tr)
    {
        if (!setup.boneToMatIndex.ContainsKey(joint.name)) return;
        var currentMat = data.matrices[setup.boneToMatIndex[joint.name]];
        var newTr = new BasicTransform
        {
            position = tr.position + (tr.rotation * (joint.translation * bodyScale)),
            rotation = tr.rotation * currentMat.rotation
        };

        if (joint.name == "Hips")
        {
            if (setWorldOffsetPeriodically && Time.time - _lastWorldOffsetTime > setWorldOffsetInterval)
            {
                _lastWorldOffsetTime = Time.time;
                worldOffset = -currentMat.GetPosition() * translationScale;
                worldOffset.y = 0f;
            }
            
            newTr.position += currentMat.GetPosition() * translationScale + worldOffset;
        }
        
        if (!_worldSpaceBoneTransforms.ContainsKey(joint.name))
        {
            _worldSpaceBoneTransforms.Add(joint.name, newTr);
        }
        else
        {
            _worldSpaceBoneTransforms[joint.name] = newTr;
        }

        foreach (var c in joint.children)
        {
            UpdateBoneTransform(c, newTr);
        }
    }
}
