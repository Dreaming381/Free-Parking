# Free Parking – Setup

Welcome! We are so excited to see what you create, no matter how big or small!

If you are already familiar with forking a Unity project, you can skip to
[here](#_The_Project_Assets).

## Forking the Project

First, you will need a GitHub account. Got to <https://github.com/> and click
“Sign Up” in the top right. Follow the prompts to create your account.

You will also need a git client. If you don’t have one, you can get a free one
provided by GitHub at <https://desktop.github.com/> and follow [this
page](https://docs.github.com/en/desktop/installing-and-authenticating-to-github-desktop/authenticating-to-github-in-github-desktop)
to connect it with your GitHub account. Make sure GitHub desktop is open.

Follow [this
guide](https://docs.github.com/en/get-started/quickstart/fork-a-repo#forking-a-repository)
to create a fork of the repository, except instead of the octocat.Spoon-Knife
repository, go to Free Parking instead. If you wish to experiment with or
contribute new features to the Latios Framework, make sure to leave “Copy the
DEFAULT branch only” unchecked. You do not need to follow the steps “Cloning
your forked repository”. There is a simpler way.

In your GitHub account, you should have your forked project. You were likely
taken to this page after creating the fork. Hit the green “Code” button, and
then choose “Open in GitHub Desktop”. Your browser may ask you if you want to
open a file in GitHub Desktop. Confirm this.

Follow the prompts in GitHub desktop to choose a location on your disk to save
your copy of the Free Parking project.

## Opening the Project

In Unity Hub under the Projects tab, click the dropdown arrow next to the “Open”
button and choose “Add project from disk”. Navigate to where you saved your
project in GitHub desktop. Select the folder that directly contains the Assets
folder of Free Parking. Unity will add the project to your list of projects and
allow you to open it. The first time the project opens may take a while, as
Unity must build the Library folder cache.

## The Project Assets

The Assets folder contains only a small number of base directories.

![](media/333e8cd85a6028f8f4acb9c5c23e765c.png)

Bootstrap contains the title, main menu, and credits scenes, along with the base
code that ties the whole project together. You will need to enter this directory
when entering your name into the credits. But otherwise you likely won’t need to
touch anything in here.

Dev Dungeons contains all the scenes, code, prefabs, created assets, and other
resources used by the dev dungeons. The Dev Dungeons are grouped by author name
and then by dungeon. If you plan to create your own dev dungeon, you will work
mostly in here.

Imports contains all imported assets from external sources (assets that were
downloaded from the internet or created in a third-party tool). If you plan to
add new assets to the project that aren’t created directly in the Unity Editor,
you will need to add them in here.

Main World contains the scene and code that drives the core gameplay experience.
If you want to help populate the world, simply open the Main World Scene and
start editing.

## Playing the Game

The main gameplay loop is not complete yet. It is recommended you open up a Dev
Dungeon scene and enter play mode from there.

## Adding Your Own Touch

The fun is just getting started. Check out one of these guides to see how to
contribute:

-   [Importing Assets](Importing%20Assets.md)
-   [Decorating the Main World](Decorating%20the%20Main%20World.md)
-   [Creating a Dev Dungeon](Creating%20a%20Dev%20Dungeon%20Manually.md)
-   Creating Activities - Todo
