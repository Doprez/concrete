#version 330 core

layout (location = 0) in vec3 aPosition;

uniform mat4 viewProj;

out vec3 worldPosition;

void main()
{
    worldPosition = aPosition;
    gl_Position = viewProj * vec4(aPosition, 1.0);
}