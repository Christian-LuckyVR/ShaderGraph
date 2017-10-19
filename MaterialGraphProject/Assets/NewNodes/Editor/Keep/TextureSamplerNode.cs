using System.Linq;
using System.Reflection;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Texture/Sample 2D")]
    public class Sample2DTexture : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRId = 4;
        public const int OutputSlotGId = 5;
        public const int OutputSlotBId = 6;
        public const int OutputSlotAId = 7;
        public const int TextureInputId = 1;
        public const int UVInput = 2;
        public const int SamplerInput = 3;

        const string kOutputSlotRGBAName = "RGBA";
        const string kOutputSlotRName = "R";
        const string kOutputSlotGName = "G";
        const string kOutputSlotBName = "B";
        const string kOutputSlotAName = "A";
        const string kTextureInputName = "Tex";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";

        public override bool hasPreview { get { return true; } }

        public Sample2DTexture()
        {
            name = "Sample2DTexture";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotRGBAId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output,0));
            AddSlot(new Vector1MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, 0));
            AddSlot(new Texture2DMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName, SlotType.Input));
            AddSlot(new Vector2MaterialSlot(UVInput, kUVInputName, kUVInputName, SlotType.Input, Vector4.zero));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            RemoveSlotsNameNotMatching(new[] { OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, TextureInputId, UVInput, SamplerInput });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //Texture input slot
            var textureSlot = FindInputSlot<MaterialSlot>(TextureInputId);
            var edgesTexture = owner.GetEdges(textureSlot.slotReference);
            // if no texture connected return nothing
            if (!edgesTexture.Any())
            {
                visitor.AddShaderChunk(precision + "4 " + GetVariableNameForSlot(OutputSlotRGBAId) + " = " + precision + "4(0,0,0,0);", true);
                return;
            }

            //UV input slot
            var uvSlot = FindInputSlot<MaterialSlot>(UVInput);
            var uvName = string.Format("{0}.xy", UVChannel.uv0.GetUVName());
            var edgesUV = owner.GetEdges(uvSlot.slotReference);
            if (edgesUV.Any())
                uvName = GetSlotValue(UVInput, generationMode);

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInput);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            string result;
            if (edgesSampler.Any())
            {
                result = string.Format(@"
#ifdef UNITY_COMPILER_HLSL
{0}4 {1} = {2}.Sample({3}, {4});
#else
{0}4 {1} = UNITY_SAMPLE_TEX2D({2}, {4});
#endif"
                        , precision
                        , GetVariableNameForSlot(OutputSlotRGBAId)
                        , GetSlotValue(TextureInputId, generationMode)
                        , GetSlotValue(SamplerInput, generationMode)
                        , uvName);
            }
            else
            {
                result = string.Format("{0}4 {1} = UNITY_SAMPLE_TEX2D({2},{3});"
                        , precision
                        , GetVariableNameForSlot(OutputSlotRGBAId)
                        , GetSlotValue(TextureInputId, generationMode)
                        , uvName);
            }

            visitor.AddShaderChunk(result, true);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.r;", precision, GetVariableNameForSlot(OutputSlotRId), GetVariableNameForSlot(OutputSlotRGBAId)), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.g;", precision, GetVariableNameForSlot(OutputSlotGId), GetVariableNameForSlot(OutputSlotRGBAId)), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.b;", precision, GetVariableNameForSlot(OutputSlotBId), GetVariableNameForSlot(OutputSlotRGBAId)), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.a;", precision, GetVariableNameForSlot(OutputSlotAId), GetVariableNameForSlot(OutputSlotRGBAId)), true);
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            if (channel != UVChannel.uv0)
            {
                return false;
            }

            var uvSlot = FindInputSlot<MaterialSlot>(UVInput);
            if (uvSlot == null)
                return true;

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();
            return edges.Count == 0;
        }
    }
}