# MattsPullRequestHelper

A simple GitHub Action to help when reviewing a Pull Request.

The aim is to help make reviewing code as easy as possible and help avoid missing something important. These are things I look for as part of a review but haven't found any other existing tooling that can do this.

Current functionality:

- Report the number of tests added. (If low or none, may need to ask why or it could indicate the PR isn't ready for review.)
- Report the number of tests deleted. (If there are any, it should raise questions.)
- Report any public methods that have been removed. (Depending on the project this may need to be documented or avoided.)

Written in C# and intended for reviewing C# files.
