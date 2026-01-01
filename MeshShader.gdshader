shader_type spatial;
// render_mode unshaded,wireframe,cull_disabled;
// uniform vec4 albedo : source_color = vec4(0.0,0.0,0.0,1.0);


void vertex() {
	vec4 clip_position = PROJECTION_MATRIX * (MODELVIEW_MATRIX * vec4(VERTEX, 1.0));
	POSITION = clip_position;
}

void fragment() {
	// ALBEDO = albedo.rgb;
  ALBEDO = source_color;
}
