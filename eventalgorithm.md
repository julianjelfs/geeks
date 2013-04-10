Event Algorithm
===============

Assuming we have a group of people who are invited and have all mutually ranked each other then we need to go through the following phases

###1) Exclude people through rules###
Always record the reason that someone was excluded so that we can check the results. 

When people respond to an invite they will specify whether they are available to go or not and they may also specify `only-if` and or `not-if` constraints.

Anyone who said no is added to the excluded list immediately and removed from all other people's `not-if` constraints. 

####1.1) Process `only-ifs`
If a person has an excluded person in their `only-if` constraint then they too are excluded.

####1.2) Process `not-ifs`
If a person has someone in their `not-if` constraint who is *not* excluded, then they are excluded. The problem here is that this `not-if` person may go on to be excluded later. Not sure how to deal with that.
