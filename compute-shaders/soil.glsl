#[compute]
#version 450

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

struct Cell {
    float nutrient;
    uint present;
};

layout(set = 0, binding = 0, std430) readonly buffer InputState {
    Cell cells[];
} input_state;

layout(set = 0, binding = 1, std430) writeonly buffer OutputState {
    Cell cells[];
} output_state;

layout(push_constant, std430) uniform Params {
    uint width;
    uint height;
    float diffusion_rate;
} params;

uint index_of(uint x, uint y)
{
    return y * params.width + x;
}

bool is_inside(ivec2 p)
{
    return p.x >= 0 &&
           p.y >= 0 &&
           p.x < size.x &&
           p.y < size.y;
}

bool is_soil(ivec2 p)
{
    return InputState[index_of(pos.x, pos.y)].present == 1;
}

void main()
{
    ivec2 pos = ivec2(gl_GlobalInvocationID.xy);

    Cell cell = InputState[index_of(pos.x, pos.y)];

    // Don't process invocations outside the image.
    if (!is_inside(pos))
        return;

    // Non-soil cells are simply copied unchanged.
    if (cell.present == 0)
    {
        OutputState[index_of(pos.x, pos.y)] = Cell{0.0, 0};
        return;
    }

    float delta = 0.0;

    for (int dx = -1; dx <= 1; dx++)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            // Don't compare the cell with itself.
            if (dx == 0 && dy == 0)
                continue;


            ivec2 neighbor_pos = pos + ivec2(dx, dy);


            // Outside the world.
            if (!is_inside(neighbor_pos))
                continue;


            // Nutrient only diffuses through soil.
            if (!is_soil(neighbor_pos))
                continue;


            float neighbor_nutrient = InputState[index_of(neighbor_pos.x, neighbor_pos.y)]


            float weight;

            // weight of cardinal is stronger
            if (dx == 0 || dy == 0)
                weight = 1.0;
            else
                weight = 0.5;


            // this delta system should conserve nutrients because it is symetric
            delta +=
                (neighbor_nutrient - cell.nutrient) *
                weight;
        }
    }

    float new_nutrient =
        cell.nutrient +
        params.diffusion_rate * delta;

    new_nutrient = max(new_nutrient, 0.0);

    cell.nutrient = new_nutrient;

    OutputState[index_of(pos.x, pos.y)] = cell;
}