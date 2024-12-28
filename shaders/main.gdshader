shader_type spatial;

uniform float outline_width : hint_range(0.0, 10.0, 0.1) = 2.0;
uniform vec3 base_color : source_color = vec3(1.0);

float fresnel(float amount, vec3 normal, vec3 view)
{
	float dot_product = dot(normal, view);
	float clamp_result = clamp(dot_product, 0.0, 1.0);
	return pow((1.0 - clamp_result), amount);
}

void fragment()
{
	ALBEDO = base_color;
}

void light()
{
	float basic_fresnel = step(0.5, fresnel(outline_width, NORMAL, VIEW));
	float opposite_basic_fresnel = step(basic_fresnel, 0.5);
	float normal_light_dot = dot(NORMAL, LIGHT);
	float is_not_shadow_1 = step(0.45, normal_light_dot);
	float is_not_shadow_2 = step(0.3, normal_light_dot);
	float is_not_shadow_3 = step(0.15, normal_light_dot);
	float is_not_shadow_4 = step(0.0, normal_light_dot);
	float is_not_shadow_result = (is_not_shadow_1 + is_not_shadow_2 + is_not_shadow_3 + is_not_shadow_4) / 4.0;
	float attenuation_1 = step(1.0, ATTENUATION);
	float attenuation_2 = step(0.4, ATTENUATION);
	vec3 glare_color = LIGHT_COLOR * 0.7;
	vec3 shadow_outline_color = vec3(-0.01, -0.01, -0.01);
	
	// Default light
	float is_active = clamp(clamp(opposite_basic_fresnel - attenuation_1 + 1.0, 0.0, 1.0) + is_not_shadow_2 + is_not_shadow_3 + is_not_shadow_4, 0.0, 1.0);
	DIFFUSE_LIGHT += is_active * is_not_shadow_result * ((attenuation_1 + attenuation_2) / 2.0) * LIGHT_COLOR;
	// Glare
	DIFFUSE_LIGHT += basic_fresnel * glare_color * is_not_shadow_result * attenuation_1;
	// Shadow outline
	DIFFUSE_LIGHT += basic_fresnel * shadow_outline_color * step(normal_light_dot, 0.45);
}