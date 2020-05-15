package net.ijbrown.bgtools.vifview;

import org.joml.Vector3f;

/**
 * Something that has a position, rotation and scale.
 */
public interface IGameItem {
     Vector3f getRotation();
     Vector3f getPosition();
     float getScale();

     void setPosition(float x, float y, float z);

     void render(ShaderProgram shaderProgram);
}
