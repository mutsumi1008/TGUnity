# TGUnity
C# script for Unity, which connects to Neurosky's ThinkGear  (e.g. Mindwave) and gather EEG data.

# Requirement
Requires ThninkGearConnector.

# Usage
Make empty game object (or, whatever) in Unity, and apply the script.
Other notes in script (see comments).

# Note
Might need to change some parameters (int bufferSize: larger, int ThreadSleepTime: smaller) if JSON parse error occurs.