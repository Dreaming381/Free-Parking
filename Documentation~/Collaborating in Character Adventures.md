# Collaborating in Character Adventures

Character Adventures is a development effort within Free Parking to build out
all the tooling required to bring characters to life within the Latios
Framework. These include things like physics character controllers, animation
systems, IK layers, procedural effects, and much more.

Every project has unique needs. Collaborating in Free Parking is the surest way
to have those needs met within the Latios Framework. However, the burden is on
you to take initiative and properly leverage this free resource and opportunity.
You **will** be expected to provide an open-art character, animations, and
environment that closely approximates your project’s needs. And you will need to
maintain an open line of communication and participate in the feedback and
iteration loop to ensure the results are satisfactory for your project. You will
**not** be expected to understand the mathematics. And if you are not a
programmer, you will not be expected to write code. You also will not be
expected to provide the full set of animations and abilities of your character
at the beginning. This can be an incremental process and changes will be
tolerated.

It does not matter if you are a professional team, or a hobbyist messing around.
I’m willing to work with whoever is willing to work with me.

Now that the ground rules are established, let’s get started!

## Designing a Character and Environment for Free Parking

The very first step is creating an open-art character and environment for Free
Parking. The term “open-art” means that it is legal for the art to be shared in
its raw form within a public GitHub repository. Many 3D asset stores disallow
this, so be cautious.

While highly technical in nature, character controllers and IK solutions are
still require artistic touches. You will want to ensure that the art you provide
provides a good approximation for the actual characters and scenarios you intend
to use the solution for. Make sure to add detail where such details will matter
for your real use case. You do not need to provide more effort than what you
believe is necessary for this approximation. However, artwork that goes above
and beyond will help the Latios Framework and the community continue to develop
long after the collaboration has reached a satisfactory conclusion.

You do not need to provide the full set of art initially. This Character
Adventures collaboration process is designed to account for redesigns,
iterations, and improvements. The environment can be especially basic and built
out of primitives to start with. You can also leverage the open-art assets
already present in Free Parking to build your test scene.

To get your character and custom environment assets into Free Parking, you will
want to follow [this guide](Importing%20Assets.md). Be sure to import your
character with the correct import settings for your needs (exposed or
optimized).

## Choosing the Right Development Space

Character Adventures development primarily happens within Free Parking’s “Dev
Dungeons”. These are isolated environments from the rest of the project to
explore various mechanisms within the Latios Framework. There are two options
within this space where development of your character can happen. If you are not
a programmer, this will be chosen for you.

There is a single shared dev dungeon named “Characters Only” which provides a
simplified workflow for collaboration. Here, each character is separated into
four layers:

-   Input
-   Movement
-   Animation
-   IK

If the needs of your character’s solution fall within these four buckets, then
working within the Characters Only dungeon may be the best choice for you.

The other option is to build your own dev dungeon within the Character
Adventures directory. This allows you to add basic gameplay logic to your
environment, as well as structure your character’s layers in a way that
more-closely matches your own project’s needs. This option is not recommended if
you intend to use Unity Transforms and Unity Physics, as Free Parking uses QVVS
Transforms and Psyshock.

While some work may be required, you may be able to change your decision during
the development process. There’s no need to overthink this step.

If you have decided to go the “Characters Only” route, read on to the next
section. Otherwise, skip to the “Custom Dev Dungeons” section.

## Characters Only Process

In the project window, navigate to Assets/Dev
Dungeons/_CharacterAdventures/_CharactersOnly and take a look around. In the
Scenes folder, you’ll find a CharacterAdventures_CharactersOnly_Example scene.
Open it up, and enter play mode. Use the WASD keys to observe the behavior.

This example demonstrates the four layers required by each character. There’s
the input layer which captures the WASD keys, There’s the movement layer which
allows the cube to move around. There’s the animation layer which causes the
cube to spin when it decelerates. And there’s the IK layer which is what
provides the tilting behavior of the cube.

In the Prefab Characters folder, you will find four prefabs containing each of
these four layers as prefab variants. Each layer adds additional authoring
components associated with its responsibility. You will want to mimic these four
layers for your own character.

*Q: Why is everything separated like this?*

*A: This will allow us to iterate on different parts of the character without
encountering merge conflicts.*

### Configuring Your Assets

First, create a new scene named
CharacterAdventures_CharactersOnly_YourCharacter, replacing “YourCharacter” with
the actual name of your character (assume this for all further references of
“YourCharacter”). Save this scene next to the example scene in the Scenes
folder.

Second, create a subscene in the scene. Feel free to give it any name you want.

Third, create your input-layer character prefab in the subscene, minus the
dedicated authoring component. Name this prefab YourCharacterInput. Drag this
into the Prefab Characters folder and delete the instance from the scene.

Fourth, create a prefab variant from YourCharacterInput and name it
YourCharacterMovement. Then create a prefab variant from YourCharacterMovement
and name it YourCharacterAnimation. And from YourCharacterAnimation, create a
variant named YourCharacterIK.

Fifth, add YourCharacterIK into the subscene.

Changes made to the character in the scene may be discarded during git merges,
so you always want to modify the prefabs when iterating on parameters.

Sixth, create an Input Actions Map for your character in the Input directory.
Name it YourNameInputActions.

Seventh, add any custom environments into the Prefab Environments directory.

If you are not a programmer, you are done at this point. And you should make a
pull request.

If you are a programmer, you can help push the character further towards your
use case by constructing the input and animation layers in code. These steps can
be done incrementally.

### Code Layers

Code is organized structurally into directories. The Systems directory uses a
separate assembly from Authoring and Components for faster iteration time.

First, navigate to the Components directory and create a
YourCharacterComponents.cs file. In it, define an `IComponentData` named
`YourCharacterDesiredActions`. Populate it with any input values you would like
the motion controller to receive. You may also create another `IComponentData`
named `YourCharacterAnimationMovementOutput`. This component should contain any
information you would like to receive from the movement layer for animation
purposes.

You may also use this file to create any additional components needed for input
and animation. You can wrap all these components within a \#region block to
assist with git merging. I will be adding the movement and IK components to this
file in a separate `#region` block during our collaboration.

Second, navigate to the Authoring folder and create two authoring scripts named
YourCharacterInputAuthoring.cs and YourCharacterAnimationAuthoring.cs. In the
bakers, add the `IComponentData` you defined to the entity. Then attach these
authoring components to the representative prefabs.

Third, navigate to the Systems folder and create your associated system scripts.
Typically, you’d name these YourCharacterInputSystem.cs and
YourCharacterAnimationSystem.cs. However, if you need to divide animation into
multiple systems, you can instead name the system
YourCharacterAnimationCustomSuffixSystem.cs where “CustomSuffix” is replaced
with some designator for the role that system plays within your solution.

Implement your systems with your required logic. Your systems can be
`SystemBase`, `ISystem`, or `SubSystem` types. You can also choose to schedule
jobs or run everything on the main thread. Performance is not critical here as
these systems will only run for a tiny number of characters and only within this
dev dungeon scene.

Lastly, open CharacterOnlySuperSystem.cs and inside `CharacterOnlySuperSystem`’s
`CreateSystems()` method, add the appropriate lines to add your newly-created
systems. The input system should be ordered before the animation system.

Be sure to test your systems to ensure they function as intended with your
character (at least the parts that don’t require functioning movement layer
outputs).

Once you are ready, make a pull request!

## Custom Dev Dungeons

There are two reasons you ended up here. Either you made a bunch of really
awesome thematic art and gave me liberty to create something with it (and are
sticking around to help with tuning), or you are a programmer with some complex
needs. If you are the former, you can ignore this step as I will probably
provide custom simplified instructions through our designated communication
channel.

Unlike with Characters Only where everything is named after your character, the
custom dev dungeon will use a name that represents your team. You can have
multiple characters within your dev dungeon, and they will share behaviors where
possible.

Create a folder inside of Assets/Dev Dungeons/_CharacterAdventures named after
your team name and create the base of your dev dungeon inside this folder, by
following the dev dungeon guide.

For characters, create prefab variants to designate specific spaces you expect
me to work with, and discuss intended inputs and outputs with me through our
designated communication channel.

It is strongly recommended you create your own assembly definition for systems.
You may additionally choose to create assembly definitions for authoring and
components as well.

## What Will Collaboration Look Like?

There may be multiple collaborators that I will be tailoring to. My time is
limited, and I also have my own priorities. I ask that you be patient with me.

The solutions I will be developing will not go directly into the Latios
Framework. Rather, they will exist here in Free Parking as examples. What will
go into the framework are common building blocks discovered through this process
that help form these solutions. Over time, patterns will likely emerge and
Latios Framework will provide more and more high-level functionality. But to
start with, Free Parking will be the go-to place if you are looking for a full
character solution. And such solutions are not guaranteed to work in other
projects or with different types of characters.

I’ll be experimenting with lots of different ideas and techniques through this
process. Some will be stupid disasters, and some will be brilliant surprises. We
may decide to create additional layers and prefab variants for better testing
and tuning. We may have multiple instances of the characters in the subscenes
for A/B testing. We may have multiple variants of an authoring component for
different algorithms, or perhaps we combine the authoring experience with an
enum and perform the divergence during baking. Each character and each project
may be slightly different depending on what works best. This won’t be a strict
process.

I intend to document my development process in the Latios Framework
Documentation repo in a folder titled Character Adventures. My hope is that the
information provided there will help everyone better understand how these
solutions work and be able to migrate and modify these solutions for their own
use cases.

I will periodically ask for feedback on the current state of particular
solutions I come up with. And sometimes I will ask that you take the time to
tune parameters to see if they provide the results you are looking for. If I am
waiting for you to respond, I will likely not be working on our collab. This
does not mean I have given up and lost interest. It just means I am skeptical if
any further work I do will be beneficial to your goals and do not wish to waste
my time and effort if it won’t be.

At first, the character and environments I need from you can be simple. We’ll
build a solution that works with the simple cases first, and then I may ask you
to provide some of the more complicated cases for your needs, such as trickier
terrain or complex animation sequences with interrupts that have independent IK
needs and targets. We will continue this cycle until you choose that the
solution is satisfactory. And if at some point you reach this point, but then
discover an additional failure point, we can continue where we left off. I have
no strict timelines for collaboration. My timelines will only dictate what
features land in particular framework releases.

If I ever say something that makes you uncomfortable, I promise you, I am not
trolling, and it is not intentional. I ask that you bring this to my attention
bluntly so that I can correct my behavior. I am a very technical person, and
sometimes I choose words that have connotations that do not reflect my true
intent and feelings. Hopefully we won’t have any issues, but I write this as a
precautionary measure.

Anyways, I am really looking forward to these collaborations! Bringing
characters to life is one of the main objectives of the Latios Framework. And
while I am still far away from being able to bring my own characters to life, it
is exciting to help get an early start on this goal by helping you with yours!

Let’s stay in touch!
