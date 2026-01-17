// NOTE: Shader automatically converted from Godot Engine 4.5.1.stable.mono's StandardMaterial3D.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

const int MAX_LAYER_COUNT = 8;
const float EPSILON = 0.0001;

uniform int layerCount;
uniform vec4 baseColors[MAX_LAYER_COUNT];
uniform float baseStartHeights[MAX_LAYER_COUNT];
uniform float baseBlends[MAX_LAYER_COUNT];
uniform float baseColorStrengths[MAX_LAYER_COUNT];
uniform float baseTextureScales[MAX_LAYER_COUNT];
uniform sampler2DArray baseTextures: hint_default_white, filter_linear_mipmap, repeat_enable;

uniform float minHeight;
uniform float maxHeight;

uniform sampler2D test_texture : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform float test_scale = 1.0;

// uniform vec4 albedo : source_color;
// uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;
// uniform ivec2 albedo_texture_size;
// uniform float point_size : hint_range(0.1, 128.0, 0.1);

// uniform float roughness : hint_range(0.0, 1.0);
// uniform sampler2D texture_metallic : hint_default_white, filter_linear_mipmap, repeat_enable;
// uniform vec4 metallic_texture_channel;
// uniform sampler2D texture_roughness : hint_roughness_r, filter_linear_mipmap, repeat_enable;

// uniform float specular : hint_range(0.0, 1.0, 0.01);
// uniform float metallic : hint_range(0.0, 1.0, 0.01);

// uniform vec3 uv1_scale;
// uniform vec3 uv1_offset;
// uniform vec3 uv2_scale;
// uniform vec3 uv2_offset;

varying vec3 local_position;
varying vec3 local_normal;
varying vec4 world_position;
varying vec3 world_normal;

void vertex() {
  local_position = VERTEX;
  local_normal = NORMAL;
  world_position = MODEL_MATRIX * vec4(VERTEX, 1.0);
  world_normal = MODEL_NORMAL_MATRIX * NORMAL;
	// UV = UV * uv1_scale.xy + uv1_offset.xy;
}

float inverseLerp(float a, float b, float value) {
  if (a == b) {
    return 0.0;
  }
  return clamp((value - a) / (b - a), 0.0, 1.0);
}

int findColorIndex(float height) {
  for(int index = layerCount - 1; index >= 0; index--) {
    if (height >= baseStartHeights[index]) {
      return index;
    }
  }
  return 0;
}

vec3 triplanarTextureColor(int index, vec3 worldPos, vec3 blendAxes, float scale) {
  vec3 p = worldPos.xyz / scale;
  vec3 xProjection = texture(baseTextures, vec3(p.y, p.z, float(index))).rgb * blendAxes.x;
  vec3 yProjection = texture(baseTextures, vec3(p.x, p.z, float(index))).rgb * blendAxes.y;
  vec3 zProjection = texture(baseTextures, vec3(p.x, p.y, float(index))).rgb * blendAxes.z;
  return xProjection + yProjection + zProjection;
}

void fragment() {
  ALBEDO = vec3(0.0, 0.0, 0.0);
  if (world_position.y > maxHeight) {
    ALBEDO = vec3(1.0, 0.0, 0.0);
  } else if (world_position.y < minHeight) {
    ALBEDO = vec3(0.0, 1.0, 0.0);
  } else {
    float heightPercent = inverseLerp(minHeight, maxHeight, world_position.y);
    vec3 blendAxes = abs(world_normal);
    blendAxes /= (blendAxes.x + blendAxes.y + blendAxes.z);

    for(int index = 0; index < layerCount; index++) {
      // float drawStrength = clamp(sign(heightPercent - baseStartHeights[index]), 0, 1);
      float drawStrength = inverseLerp(
        -baseBlends[index]/2.0 - EPSILON, 
        baseBlends[index]/2.0,
        heightPercent - baseStartHeights[index]
      );
      vec3 baseColor = baseColors[index].rgb * baseColorStrengths[index];
      vec3 textureColor = triplanarTextureColor(index, world_position.xyz, blendAxes, baseTextureScales[index]);
      textureColor *= (1.0 - baseColorStrengths[index]);

      ALBEDO = ALBEDO * (1.0 - drawStrength) + (baseColor + textureColor) * drawStrength;
      
    }
  }

}
