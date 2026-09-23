using Godot;
using System;
using System.Data;
using System.IO;


public class SoilShaderHandler
{
    private RenderingDevice rd;
    private RDShaderFile shaderFile;
    private RDShaderSpirV shaderBytecode;
    public Rid shader;

    private Image inputImage;

    public void Initialize()
    {
        rd = RenderingServer.CreateLocalRenderingDevice();
        shaderFile = GD.Load<RDShaderFile>("res://compute_example.glsl");
        shaderBytecode = shaderFile.GetSpirV();
        shader = rd.ShaderCreateFromSpirV(shaderBytecode);
    }


    public void SetShaderData(Element[,] elementArray, int width, int height){
        // Prepare the data for the shader. red channel is nutrients, green channel is at 1.0 for soil
        // Ideally we could update the image when changes are made to the soil to avoid having to iterate through the entire array every frame, but for now this is fine.
        inputImage = new Image();
        // Set the image data here
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Element element = elementArray[x, y];
                float nutrients = 0.0f;
                float soilPresence = 0.0f;

                if (element is Soil)
                {
                    nutrients = ((Soil)element).nutrient;
                    soilPresence = 1.0f;
                }

                // Set the pixel color based on the element properties
                Color pixelColor = new Color(nutrients, soilPresence, 0.0f, 1.0f);
                inputImage.SetPixel(x, y, pixelColor);
            }
        }
    }

    public Image getResult(){
        // To be called a few frames after RunCompute() to get the result of the compute shader.

    }

    public void RunCompute(){
        // Prepare our data. We use floats in the shader, so we need 32 bit.

        Image.BlockCopy(input, 0, inputBytes, 0, inputBytes.Length);

        // Create a storage buffer that can hold our float values.
        // Each float has 4 bytes (32 bit) so 10 x 4 = 40 bytes
        var buffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);
    }

    public void InitGPU(){
        //# These resources are expensive to make, so create them once and cache for subsequent runs.

        //# Create a local rendering device (required to run compute shaders).
        rd = RenderingServer.CreateLocalRenderingDevice();

        if (rd == null) {
            GD.PushWarning("""Couldn't create local RenderingDevice on GPU""");
        }

        //# Prepare the shader.
        shaderFile = GD.Load<RDShaderFile>("res://compute_example.glsl");

        //# Create format for heightmap.
        RDTextureFormat input_format = new RDTextureFormat();
        //# There are a lot of different formats. It might take some studying to be able to be able to
        //# choose the right ones. In this case, we tell it to interpret the data as a single byte for red.
        //# Even though the noise image only has a luminance channel, we can just interpret this as if it
        //# was the red channel. The byte layout is the same!
        heightmap_format.format = RenderingDevice.DataFormat.rgb
        heightmap_format.width = po2_dimensions
        heightmap_format.height = po2_dimensions
        //# The TextureUsageBits are stored as 'bit fields', denoting what can be done with the data.
        //# Because of how bit fields work, we can just sum the required ones: 8 + 64 + 128
        heightmap_format.usage_bits = \
                RenderingDevice.TEXTURE_USAGE_STORAGE_BIT + \
                RenderingDevice.TEXTURE_USAGE_CAN_UPDATE_BIT + \
                RenderingDevice.TEXTURE_USAGE_CAN_COPY_FROM_BIT

        //# Prepare heightmap texture. We will set the data later.
        heightmap_rid = rd.texture_create(heightmap_format, RDTextureView.new())

        //# Create uniform for heightmap.
        var heightmap_uniform := RDUniform.new()
        heightmap_uniform.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
        heightmap_uniform.binding = 0  //# This matches the binding in the shader.
        heightmap_uniform.add_id(heightmap_rid)

        //# Create format for the gradient.
        var gradient_format := RDTextureFormat.new()
        //# The gradient could have been converted to a single channel like we did with the heightmap,
        //# but for illustrative purposes, we use four channels (RGBA).
        gradient_format.format = RenderingDevice.DATA_FORMAT_R8G8B8A8_UNORM
        gradient_format.width = gradient_tex.width  //# Default: 256
        //# GradientTexture1D always has a height of 1.
        gradient_format.height = 1
        gradient_format.usage_bits = \
            RenderingDevice.TEXTURE_USAGE_STORAGE_BIT + \
            RenderingDevice.TEXTURE_USAGE_CAN_UPDATE_BIT

        //# Storage gradient as texture.
        gradient_rid = rd.texture_create(gradient_format, RDTextureView.new(), [gradient_tex.get_image().get_data()])

        //# Create uniform for gradient.
        var gradient_uniform := RDUniform.new()
        gradient_uniform.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
        gradient_uniform.binding = 1  # This matches the binding in the shader.
        gradient_uniform.add_id(gradient_rid)

        uniform_set = rd.uniform_set_create([heightmap_uniform, gradient_uniform], shader_rid, 0)

        pipeline = rd.compute_pipeline_create(shader_rid)
    }

    public void CleanupGPU(){
        if (rd == null) {
            return;
        }

        rd.FreeRid(pipeline)
        pipeline = RID()

        rd.free_rid(uniform_set)
        uniform_set = RID()

        rd.free_rid(gradient_rid)
        gradient_rid = RID()

        rd.free_rid(heightmap_rid)
        heightmap_rid = RID()

        rd.free_rid(shader_rid)
        shader_rid = RID()

        rd.free()
        rd = null

}

