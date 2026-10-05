shader_type canvas_item;
render_mode unshaded;

uniform sampler2D color_texture : source_color, filter_nearest;
uniform vec2 grid_size = vec2(1.0, 1.0);
uniform vec2 cell_size = vec2(1.0, 1.0);
uniform vec2 zoomed_window_origin;
uniform vec2 zoomed_window_size = vec2(1.0, 1.0);

void fragment() {
	vec2 visible_uv = UV * zoomed_window_size;
	vec2 cell_index = floor(visible_uv + zoomed_window_origin);
	cell_index = clamp(cell_index, vec2(0.0), grid_size - vec2(1.0));
	vec2 sample_uv = (cell_index + vec2(0.5)) / grid_size;
	COLOR = texture(color_texture, sample_uv);
}
