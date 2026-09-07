shader_type spatial;
render_mode unshaded,vertex_lighting;

uniform sampler2D SCREEN_TEXTURE: hint_screen_texture, filter_linear_mipmap;
uniform sampler2D NORMAL_TEXTURE : hint_normal_roughness_texture, filter_linear_mipmap;

// hatching
uniform sampler2D hatch: filter_nearest,repeat_enable;
uniform sampler2D hatch1: filter_nearest,repeat_enable;
uniform sampler2D hatch2: filter_nearest,repeat_enable;
uniform sampler2D hatch3: filter_nearest,repeat_enable;
uniform float hatch_uv_scaling: hint_range (0.0, 50.0, 1.0) = 20.0;

// edge lines
uniform float edge_threshold = 0.2;
uniform vec3 line_color: source_color = vec3(0.0);
uniform vec3 background_color: source_color = vec3(1.0);

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

void vertex(){
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec3 screen_color = texture(SCREEN_TEXTURE,uv).rgb;
	vec2 offset = 0.5 / VIEWPORT_SIZE;
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
	
	// working on posterize version of hatching
	//vec3 hatch_tex = texture(hatch,uv * hatch_uv_scaling).rgb;
	//float steps = 8.0;
	//float posterize = 1.0 - (floor(uv.x * steps) / (steps - 1.0)); // posterize into 4 stepped levels
	//float hatch_g = step(1.0,posterize) * hatch_tex.g;
	//float hatch_r = step(0.5,posterize) * hatch_tex.r;
	//float hatch_b = step(0.25,posterize) * hatch_tex.b;
	//float hatch_color = max(max(hatch_r,hatch_g),hatch_b);
	//
	//screen_color = vec3(hatch_r,hatch_g,hatch_b);
	
	// hatching based on luminance
	float luminance = dot(screen_color, vec3(0.299, 0.587, 0.114));
	
	vec3 final_color = screen_color;
	vec4 hatch_tex = texture(hatch,uv * hatch_uv_scaling);
	if (luminance < 0.75) {
		//final_color -= vec3(hatch_tex.g);
		//final_color -= texture(hatch1,uv * hatch_uv_scaling).rgb;
	}
	if (luminance < 0.5) {
		//final_color -= vec3(hatch_tex.r);
		//final_color -= texture(hatch2,uv * hatch_uv_scaling).rgb;
	}
	if (luminance < 0.25) {
		//final_color -= vec3(hatch_tex.b);
		//final_color -= texture(hatch3,uv * hatch_uv_scaling).rgb;
	}
	
	// apply edge lines
	if (edge > edge_threshold) {
		ALBEDO = line_color;
	} else {
		//ALBEDO = vec3(luminance,luminance,luminance);
		ALBEDO = final_color;
	}
}
