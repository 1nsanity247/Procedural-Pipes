namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using Assets.Scripts.Design;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("ProceduralPipe")]
    [PartModifierTypeId("ProceduralPipes.ProceduralPipe")]
    public class ProceduralPipeData : PartModifierData<ProceduralPipeScript>
    {
        [SerializeField]
        [DesignerPropertySlider(0.1f, 2f, 20, Header = "Front", Label = "Radius", Tooltip = "Radius of the front part of the pipe.")]
        private float _frontRadius = 0.1f;

        [SerializeField]
        [DesignerPropertySlider(0.1f, 10f, 100, Label = "Weight", Tooltip = "Determines the shape of the pipe near the front.")]
        private float _frontWeight = 1f;

        [SerializeField]
        [DesignerPropertySlider(0.1f, 2f, 20, Header = "Back", Label = "Radius", Tooltip = "Radius of the back part of the pipe.")]
        private float _backRadius = 0.1f;

        [SerializeField]
        [DesignerPropertySlider(0.1f, 10f, 100, Label = "Weight", Tooltip = "Determines the shape of the pipe near the back.")]
        private float _backWeight = 1f;

        [SerializeField]
        [DesignerPropertySlider(12, 48, 7, Label = "Resolution", IsHidden = true)]
        private int _resolution = 24;

        [SerializeField]
        [DesignerPropertySlider(12, 48, 7, Label = "Segment Count", IsHidden = true)]
        private int _segmentCount = 24;

        [SerializeField]
        [PartModifierProperty()]
        public Curve Curve;

        [SerializeField]
        [PartModifierProperty()]
        public Quaternion FrontRotation;

        [SerializeField]
        [PartModifierProperty()]
        public Quaternion BackRotation;

        public float FrontRadius { get { return _frontRadius; }}
        public float FrontWeight { get { return _frontWeight; } }
        public float BackRadius { get { return _backRadius; }}
        public float BackWeight { get { return _backWeight; } }
        public int Resolution { get { return _resolution; } }
        public int SegmentCount { get { return _segmentCount; } }

        public override float MassDry => 0.5f * (Curve == null ? 1.0f : Curve.length) * (_frontRadius + _backRadius) * 0.005f * 2700f * 0.01f;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(() => ValueChanged());
            d.OnValueLabelRequested(() => _frontRadius, (float x) => x.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _backRadius, (float x) => x.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _frontWeight, (float x) => x.ToString("n1"));
            d.OnValueLabelRequested(() => _backWeight, (float x) => x.ToString("n1"));          
        }

        void ValueChanged()
        {
            Symmetry.SynchronizePartModifiers(Script.PartScript);
            Script.UpdatePipeWeights();
            Script.GeneratePipeMesh();
            Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }
}