#version 330 core

in vec3 worldPosition;

out vec4 color;

uniform vec3 cameraPosition;

const vec3 gridColor = vec3(0.5);
const vec3 axisXColor = vec3(1.0, 0.0, 0.0);
const vec3 axisZColor = vec3(0.0, 0.0, 1.0);

const float gridSpacing = 1.0;
const float lineThickness = 1;

const float fadeDistance = 4.0;
const float fadeRange = 20.0;

void main()
{
    // distance from camera in xz plane
    vec2 relativePosition = (worldPosition.xz - cameraPosition.xz);
    float distFromCamera = length(relativePosition);

    // scale line thickness based on screen space
    vec2 uv = worldPosition.xz / gridSpacing;
    vec2 uvDeriv = fwidth(uv);
    float scaledLineThicknessX = uvDeriv.x * lineThickness;
    float scaledLineThicknessZ = uvDeriv.y * lineThickness;

    // distance to nearest grid line
    float distX = abs(fract(uv.x + scaledLineThicknessX / 2));
    float distZ = abs(fract(uv.y + scaledLineThicknessZ / 2));

    // check which line the pixel is on
    bool onXLine = distX < scaledLineThicknessX;
    bool onZLine = distZ < scaledLineThicknessZ;
    bool onAxisX = abs(worldPosition.z) < scaledLineThicknessZ / 2;
    bool onAxisZ = abs(worldPosition.x) < scaledLineThicknessX / 2;

    // calculate fading alpha
    float alpha = 1.0 - smoothstep(fadeDistance, fadeDistance + fadeRange, distFromCamera);

    // apply color
    if (onAxisZ) color = vec4(axisZColor, alpha);
    else if (onAxisX) color = vec4(axisXColor, alpha);
    else if (onXLine || onZLine) color = vec4(gridColor, alpha);
    else discard;
}