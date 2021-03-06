# HtmSpellingChecker

HtmSpellingChecker is an experimental spelling checker using HTM neural networks as approach.

The proposed system was written the in C#, as it compiles source to managed code and also is a cross-platform language. Both the C# compiler and editor are freely provided by Mono open-source community.
In addition to the subprojects that compose the proposed system, three additional subprojects were created: one to implement the Levenshtein Distance algorithm, another to implement N-Grams algorithm, and a subproject called Evaluation, with the sole intention of comparing all three methods discussed in this dissertation. As the engines of the three methods use components in common (dictionaries and text manipulation), one subproject ‘Common’ was created only to accommodate the components to be shared. All these subprojects were written in same language and platform in order to avoid wrappers and isolate performance issues.

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
    <img title="Main Form" src="Doc/MainForm.png"/>
</div>

 * Specify the data to be trained and tested. The text should not have graphic accentuation and sentences should have more than 3 words.
 * Specify the range of sets to be tested. For example, if minimum number of sentences is 50, maximum number is 200, and size/increment for each set is 50, then we will have sets with 50, 100, 150, and 200 sentences respectivelly.
 * Click 'Evaluate'.

## Understanding the results:

The tests consist of trainning 3 engines (HTM, Levenstein Distance, and N-Gram) with a corpus containing well written sentences and then test them with mispelled sentences created from the first corpus. These mispelled sentences will have at least one word that suffered an modification (insertion, deletion, replacement or transposition of characters).

Two variables will be calculated and shown:
 * Performance: in other words, is the total time that each engine takes to evaluate and return the list of corrections for a set.
 * Accuracy: is the percentage of correct suggestions returned by an engine over the real number of issues created.
