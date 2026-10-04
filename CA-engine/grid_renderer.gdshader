shader_type canvas_item;
render_mode unshaded;

uniform sampler2D color_texture : source_color, filter_nearest;
uniform sampler2D state_texture : source_color, filter_nearest;
uniform vec2 grid_size = vec2(1.0, 1.0);
uniform vec2 cell_size = vec2(1.0, 1.0);
uniform bool show_need_update_state = false;

void fragment() {
	vec2 grid_coords = UV * grid_size;
	vec2 cell_index = floor(grid_coords);
	vec2 pixel_coords = grid_coords * cell_size;
	vec2 cell_uv = mod(pixel_coords, cell_size);
	vec2 sample_uv = (cell_index + vec2(0.5)) / grid_size;
	vec4 cell_color = texture(color_texture, sample_uv);

	if (cell_color.a <= 0.0) {
		COLOR = vec4(0.0);
	} else {
		if (show_need_update_state) {
			float update_state = texture(state_texture, sample_uv).a;
			cell_color.rgb = update_state > 0.75 ? vec3(0.2, 0.85, 0.25) : vec3(0.9, 0.2, 0.2);
		} else {
			cell_color.a = 1.0;
		}

		
		COLOR = cell_color;
	}
}
