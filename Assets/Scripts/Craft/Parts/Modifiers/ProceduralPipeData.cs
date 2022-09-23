namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Assets.Scripts.Design;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using ModApi.Math;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("ProceduralPipe")]
    [PartModifierTypeId("ProceduralPipes.ProceduralPipe")]
    public class ProceduralPipeData : PartModifierData<ProceduralPipeScript>
    {
        private string prevFront, prevBack;

        [SerializeField]
        [DesignerPropertySlider(0.1f, 2f, 20, Label = "Front Radius", Tooltip = "Radius of the front part of the pipe.")]
        private float _frontRadius = 0.25f;

        [SerializeField]
        [DesignerPropertyTextInput(Label = "Input")]
        private string _frontRadiusInput = "0.25";

        [SerializeField]
        [DesignerPropertySlider(0.1f, 10f, 100, Label = "Front Weight", Tooltip = "Determines the shape of the pipe near the front.")]
        private float _frontWeight = 1f;

        [SerializeField]
        [DesignerPropertySlider(0.1f, 2f, 20, Label = "Back Radius", Tooltip = "Radius of the back part of the pipe.")]
        private float _backRadius = 0.25f;

        [SerializeField]
        [DesignerPropertyTextInput(Label = "Input")]
        private string _backRadiusInput = "0.25";

        [SerializeField]
        [DesignerPropertySlider(0.1f, 10f, 100, Label = "Back Weight", Tooltip = "Determines the shape of the pipe near the back.")]
        private float _backWeight = 1f;

        [SerializeField]
        [DesignerPropertySlider(12, 48, 7, Label = "Resolution", IsHidden = true)]
        private int _resolution = 24;

        [SerializeField]
        [DesignerPropertySlider(12, 48, 7, Label = "Segment Count", Tooltip = "The amount of mesh segments along the pipe.")]
        private int _segmentCount = 24;

        [SerializeField]
        [PartModifierProperty(true, false)]
        public Vector3[] points = new Vector3[5];

        [SerializeField]
        [PartModifierProperty(true, false)]
        public float length;

        public Curve curve = new Curve(new Vector3[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, 0f) });

        public float FrontRadius { get { return _frontRadius; }}
        public float FrontWeight { get { return _frontWeight; } }
        public float BackRadius { get { return _backRadius; }}
        public float BackWeight { get { return _backWeight; } }
        public int Resolution { get { return _resolution; } }
        public int SegmentCount { get { return _segmentCount; } }

        public override float MassDry => 0.5f * length * (_frontRadius + _backRadius) * 0.005f * 2700f * 0.01f;

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
            if(prevFront != _frontRadiusInput || prevBack != _backRadiusInput) 
            {
                if (!float.TryParse(_frontRadiusInput, out float f)) _frontRadiusInput = _frontRadius.ToString();
                else { _frontRadiusInput = Mathf.Clamp(f, 0.001f, 10f).ToString(); _frontRadius = Mathf.Clamp(f, 0.001f, 10f); }
                if (!float.TryParse(_backRadiusInput, out float b)) _backRadiusInput = _backRadius.ToString();
                else { _backRadiusInput = Mathf.Clamp(b, 0.001f, 10f).ToString(); _backRadius = Mathf.Clamp(b, 0.001f, 10f); }
            }
            else
            {
                _frontRadiusInput = _frontRadius.ToString();
                _backRadiusInput = _backRadius.ToString();
            }

            prevFront = _frontRadiusInput;
            prevBack = _backRadiusInput;

            Symmetry.SynchronizePartModifiers(Script.PartScript);
            Script.UpdatePipeWeights();
            Script.GeneratePipeMesh();
            Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }
}