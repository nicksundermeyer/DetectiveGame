shader_type spatial;
render_mode unshaded,vertex_lighting;

uniform sampler2D SCREEN_TEXTURE: hint_screen_texture, filter_linear_mipmap;
uniform sampler2D NORMAL_TEXTURE : hint_normal_roughness_texture, filter_linear_mipmap;

// hatching
uniform sampler2D hatch: filter_nearest,repeat_enable;
uniform float hatch_uv_scaling: hint_range (0.0, 100.0, 1.0) = 40.0;

// edge lines
uniform float edge_threshold = 0.2;
uniform float edge_thickness: hint_range(0.0, 3.0, 0.1) = 0.7;
uniform vec3 line_color: source_color = vec3(0.0);
uniform float lum_contrast: hint_range(0.0, 10.0, 0.1) = 5.0;
uniform float lum_offset: hint_range(0.0, 1.0, 0.01) = 0.5;

const mat3 sobel_y = mat3(
	vec3(1.0, 0.0, -1.0),
	vec3(2.0, 0.0, -2.0),
	vec3(1.0, 0.0, -1.0)
);

const mat3 sobel_x = mat3(
	vec3(1.0, 2.0, 1.0),
	vec3(0.0, 0.0, 0.0),
	vec3(-1.0, -2.0, -1.0)
);

vec3 normalize_normal(vec3 normal){
	return normalize(normal * 2.0 - 1.0);
}

float luminance(vec3 color) {
	const vec3 magic = vec3(0.2125, 0.7154, 0.0721);
	return dot(magic, color);
}

void vertex(){
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec3 screen_color = texture(SCREEN_TEXTURE,uv).rgb;
	vec2 offset = edge_thickness / VIEWPORT_SIZE;
	vec3 normal = normalize_normal(texture(NORMAL_TEXTURE, uv).rgb);
	
	// edge lines
	vec3 n = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(0.0, -offset.y)).rgb);
	vec3 s = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(0.0, offset.y)).rgb);
	vec3 e = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(offset.x, 0.0)).rgb);
	vec3 w = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(-offset.x, 0.0)).rgb);
	vec3 nw = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(-offset.x, -offset.y)).rgb);
	vec3 ne = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(offset.x, -offset.y)).rgb);
	vec3 sw = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(-offset.x, offset.y)).rgb);
	vec3 se = normalize_normal(texture(NORMAL_TEXTURE, uv + vec2(offset.x, offset.y)).rgb);
	
	mat3 surrounding_pixels = mat3(
		vec3(length(nw-normal), length(n-normal), length(ne-normal)),
		vec3(length(w-normal), length(normal-normal), length(e-normal)),
		vec3(length(sw-normal), length(s-normal), length(se-normal))
	);
	
	float edge_x = dot(sobel_x[0], surrounding_pixels[0]) + dot(sobel_x[1], surrounding_pixels[1]) + dot(sobel_x[2], surrounding_pixels[2]);
	float edge_y = dot(sobel_y[0], surrounding_pixels[0]) + dot(sobel_y[1], surrounding_pixels[1]) + dot(sobel_y[2], surrounding_pixels[2]);
	
	float edge = sqrt(pow(edge_x, 2.0)+pow(edge_y, 2.0));
	
	// hatching based on luminance
	float luminance = dot(screen_color, vec3(0.299, 0.587, 0.114));
	
	luminance = (luminance - 0.5 + lum_offset) * lum_contrast + 0.5;
	luminance = clamp(luminance, 0.0, 1.0);
	
	// add in different hatch textures at different luminance values
	vec3 final_color = screen_color;
	vec4 hatch_tex = texture(hatch,uv * hatch_uv_scaling);
	if (luminance < 0.75) {
		final_color -= vec3(hatch_tex.g);
	}
	if (luminance < 0.5) {
		final_color -= vec3(hatch_tex.r);
	}
	if (luminance < 0.25) {
		final_color -= vec3(hatch_tex.b);
	}
	
	// apply edge lines
	if (edge > edge_threshold) {
		ALBEDO = line_color;
	} else {
		ALBEDO = final_color;
	}
}
