# WixAdd
A basic UI for WiX Installs

So - I committed to try out WiX for some of our new product installs recently.
And it's great and all, and cheap. But the management of adding all of your DLLs, making Guids, cross referencing the Ids - it's a real headache to hand-code your XML.
I know that there's "heat" and all - but I've got two separate lists of DLLs (to cover two client addin versions) - so that looked like it would be a mess as well.

I've always said the best way of getting something automated is making a developer do something painful, manually for an hour. They will spend 4-5x as long automating it so that they NEVER have to do that again!

Here's the result of my time :).

It in no way does everything - WiX does everything. I'm not looking to remake Installshield (please, someone do that!). I was just struggling with easily adding all of the File and Component references.

So this works with an existing Wix WXS file, where you already have set up your basic few Product Features and DirectoryRefs. This app helps you:
1. Add Components/Files easily to your DirectoryRefs.
2. Add those Components as ComponentRefs in your features.

I hope you stumble across this, and it helps.
