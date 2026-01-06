shader_type spatial;
render_mode unshaded, cull_disabled;

varying float line_position;

void vertex() {
	// Determine if this is the start (0.0) or end (1.0) of the normal line
	// Even vertex IDs are start points, odd are end points
	line_position = float(VERTEX_ID % 2);

	// Pass through vertex position
	POSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
}

void fragment() {
	// Interpolate from red at the start to blue at the end
	vec3 start_color = vec3(1.0, 0.0, 0.0); // Red
	vec3 end_color = vec3(0.0, 0.0, 1.0);   // Blue

	ALBEDO = mix(start_color, end_color, line_position);
	ALPHA = 1.0;
}
