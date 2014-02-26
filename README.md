# HtmSpellingChecker

HtmSpellingChecker is an experimental spelling checker using HTM neural networks as approach.

The proposed system was written the in C#, as it compiles source to managed code and also is a cross-platform language. Both the C# compiler and editor are freely provided by Mono open-source community.
In addition to the subprojects that compose the proposed system, three additional subprojects were created: one to implement the Levenshtein Distance algorithm, another to implement N-Grams algorithm, and a subproject called Evaluation, with the sole intention of comparing all three methods discussed in this dissertation. As the engines of the three methods use components in common (dictionaries and text manipulation), one subproject ‘Common’ was created only to accommodate the components to be shared.

All subprojects were written in same language and platform in order to avoid wrappers and isolate performance issues.

## Installation

Currently supported platforms:
 * Windows
 * Linux
 * Mac OSX

Required tools:
 * [Mono 3.2.3](http://www.mono-project.com) or [.Net 4](http://www.microsoft.com/net) framework
 * [MonoDevelop](http://monodevelop.com/Download) or [Visual Studio](http://www.visualstudio.com/) IDE

## Running from the IDE:

 * Open the IDE executable.
 * Open 'HtmSpellingChecker.sln' solution file located on <code>/Source</code> folder.
 * Click 'Run'.

## Run the tests:

<div align="center">
    <img title="Main Form" src="https://github.com/DavidRagazzi/HtmSpellingChecker/tree/Release/MainForm.png"/>
</div>

(put Main Form image here)

 * Specify the text to be tested [1].
 * Specify the size of sets to be tested [2].
 * Click 'Test'.

## Understanding the results:

(under construction)
