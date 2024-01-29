# Decorating the Main World

The Main World Scene is where the main story of the game will take place. The
intent is for this scene to be full of environment and character assets at a
scale well-suited for ECS and the Latios Framework. Entrances to player-ready
dev dungeons will also be hidden within this scene.

## Main World Contents

The Main World Scene contains multiple subscenes. When you first open the scene,
each subscene will be “closed”. The assets will be visible in the scene and game
tabs, but they will not be editable.

To inspect, add, or modify the contents of the subscene, click “Open” in the
Inspector or tick the checkbox in the Hierarchy. The contents of the subscene
will be expanded in the hierarchy for editing.

You will find that the Main World Scene contains multiple subscenes with
different locations and types of objects inside each. You are free to modify any
of the existing subscenes, or add new subscenes.

## Prefabs and Materials

Any custom prefabs and materials can be saved in folders inside of the Main
World folder in the project.

## Environment Notes

Objects in the environments require colliders for the player to interact with.
The Latios Framework benefits from simpler collider shapes such as spheres,
capsules, and boxes. But all collider types except for terrain colliders are
supported. Marking mesh colliders as convex is also an optimization.

To make the player collide with the environment, the root of the hierarchy
requires the Environment component to be added. The component is located
directly within the Free Parking category of the Add Component menu.

## Character Notes

Characters in the world should be set up with Animators and Animator
Controllers. Additionally, characters will need Kinemation-compatible materials.
If set up correctly, the character idle animations should automatically play
when entering play mode.

For performance considerations, GPU performance may improve by using Latios
Vertex Skinning in Shader Graph. However, this is limited to simpler models with
4 weights per vertex and no blend shapes. For better CPU performance, a
character imported with an optimized hierarchy will tend to perform better. The
Latios Framework supports exporting bones with optimized hierarchies for
character attachments.

## Credits

After making your changes, go into the Bootstraps folder of the Free Parking and
open Credits.md in a text editor. Add your name under the *Level Design*
heading, leaving blank lines before and after.

## Pull Request

Finally, make a pull request with your changes. Keep an eye out in case
reviewers have questions about your work. But once your changes are accepted,
they will be merged in and everyone will get to see your handiwork.

## “I’m still inexperienced”

Most of the Latios Framework community is composed of programmers with limited
artistic capabilities. The bar is not very high. No one expects perfection. Use
this project as a way to grow and explore and hopefully not have to worry about
your computer slowing down to a crawl.

And if you are experienced, feel free to make things better, even if that means
undoing some things. We can all learn from your changes!
