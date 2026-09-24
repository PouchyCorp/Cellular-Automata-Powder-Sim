using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Cell
{
    public float Nutrient;
    public uint Present;
}

public class SoilShaderHandler
{
    private RenderingDevice rd;
    public Rid shader;

    public List<Cell> inputA;
    public List<Cell> inputB;

    public Rid RidInputA;
    public Rid RidInputB;

    public Rid pipeline;



    public void SetShaderData(Element[,] elementArray, int width, int height){
        // Prepare the data for the shader. red channel is nutrients, green channel is at 1.0 for soil
        // Ideally we could update the image when changes are made to the soil to avoid having to iterate through the entire array every frame, but for now this is fine.
        inputA = [];
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
                inputA.Add(new Cell { Nutrient = nutrients, Present = (uint)soilPresence });
            }
        }

        inputB = inputA;
    }

    public void getResult(){
        // To be called a few frames after RunCompute() to get the result of the compute shader.

    }

    public void RunCompute(){

    }

    public void InitGPU(){
        //# These resources are expensive to make, so create them once and cache for subsequent runs.

        //# Create a local rendering device (required to run compute shaders).
        rd = RenderingServer.CreateLocalRenderingDevice();

        if (rd == null) {
            GD.PushWarning("""Couldn't create local RenderingDevice on GPU""");
        }

        //# Prepare the shader.
        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://soil.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        shader = rd.ShaderCreateFromSpirV(shaderBytecode);



        uint cellSize = (uint)Marshal.SizeOf<Cell>();
        
        if (cellSize == 8) {
            GD.PushError("""Cell struct is not 8 bytes in size, the shader will not work""");
        }


        // Initialize the input and output buffers with the same size and data 
        // We try to map the data to the shader in a way that is compatible with the shader's expectations.
        long bufferSize = inputA.Count * cellSize;
        byte[] initialData = new byte[inputA.Count * cellSize];

        for (int i = 0; i < inputA.Count; i++)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(inputA[i].Nutrient),
                0,
                initialData,
                (int)(i * cellSize),
                4
            );
            Buffer.BlockCopy(
                BitConverter.GetBytes(inputA[i].Present),
                0,
                initialData,
                (int)(i * cellSize + 4),
                4
            );
        }
        RidInputA = rd.StorageBufferCreate((uint)bufferSize, initialData);
        RidInputB = rd.StorageBufferCreate((uint)bufferSize, initialData);


        RDUniform input_uniform = new RDUniform();
        input_uniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        input_uniform.Binding = 0;
        input_uniform.AddId(RidInputA);

        RDUniform output_uniform = new RDUniform();
        output_uniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        output_uniform.Binding = 1;
        output_uniform.AddId(RidInputB);


        // these will roll
        Rid uniform_set_AB = rd.UniformSetCreate([input_uniform, output_uniform], shader, 0);
        Rid uniform_set_BA = rd.UniformSetCreate([output_uniform, input_uniform], shader, 0);

        pipeline = rd.ComputePipelineCreate(shader);
    }

    public void InitChangeBuffer(){
        // Enough room for however many changes you expect in one frame.
        const int MaxChanges = 4096;

        _changeBuffer = _rd.StorageBufferCreate(
            MaxChanges * ChangeSize
        );
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
}

