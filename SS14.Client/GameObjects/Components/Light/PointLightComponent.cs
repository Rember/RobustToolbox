﻿using System;
using SS14.Client.Graphics.Lighting;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;
        public override Type StateType => typeof(PointLightComponentState);

        private ILight Light;
        [Dependency]
        private ILightManager lightManager;

        public Color Color
        {
            get => Light.Color;
            set => Light.Color = value;
        }

        public Vector2 Offset
        {
            get => Light.Offset;
            set => Light.Offset = value;
        }

        private LightState state = LightState.On;
        public LightState State
        {
            get => state;
            set
            {
                state = value;
                Light.Enabled = state == LightState.On;
            }
        }

        /// <summary>
        ///     Determines if the light mask should automatically rotate with the entity. (like a flashlight)
        /// </summary>
        public bool MaskAutoRotate { get; set; }

        /// <summary>
        ///     Local rotation of the light mask around the center origin
        /// </summary>
        public Angle Rotation
        {
            get => Light.Rotation;
            set => Light.Rotation = value;
        }

        public float Energy
        {
            get => Light.Energy;
            set => Light.Energy = value;
        }

        private float radius = 5;
        /// <summary>
        ///     Radius, in meters.
        /// </summary>
        public float Radius
        {
            get => radius;
            set
            {
                radius = FloatMath.Clamp(value, 2, 10);
                var mgr = IoCManager.Resolve<IResourceCache>();
                var tex = mgr.GetResource<TextureResource>(new ResourcePath("/Textures/Effects/Light/") / $"lighting_falloff_{(int)radius}.png");
                // TODO: Maybe editing the global texture resource is not a good idea.
                tex.Texture.GodotTexture.SetFlags(tex.Texture.GodotTexture.GetFlags() | (int)Godot.Texture.FlagsEnum.Filter);
                Light.Texture = tex.Texture;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var transform = Owner.GetComponent<IGodotTransformComponent>();
            Light.ParentTo(transform);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            if (lightManager == null)
            {
                // First in the init stack so...
                // FIXME: This is terrible.
                lightManager = IoCManager.Resolve<ILightManager>();
                Light = lightManager.MakeLight();
            }

            serializer.DataReadWriteFunction("offset", Vector2.Zero, vec => Offset = vec, () => Offset);
            serializer.DataReadWriteFunction("radius", 5f, radius => Radius = radius, () => Radius);
            serializer.DataReadWriteFunction("color", Color.White, col => Color = col, () => Color);
            serializer.DataReadWriteFunction("state", LightState.On, state => State = state, () => State);
            serializer.DataReadWriteFunction("energy", 1f, energy => Energy = energy, () => Energy);
            serializer.DataReadWriteFunction("autoRot", false, rot => MaskAutoRotate = rot, () => MaskAutoRotate);
        }

        public override void OnRemove()
        {
            Light.Dispose();
            Light = null;

            base.OnRemove();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (PointLightComponentState)state;
            State = newState.State;
            Color = newState.Color;
            Light.ModeClass = newState.Mode;
        }
    }
}
