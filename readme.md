# What is ARMaker?
ARMaker is a simple little project I threw together one day when I needed to generate some professional looking AR targets for Vuforia but wanted to have a high number of tracking points. I have since extended it to also include a generator for ARToolkit markers following their recommended format.

The project itself was designed to be used as an API for generating AR markers. It was built using C# and NancyFX so it could be hosted on any environment via Mono. There is a hosted version of the API available for you to use at [https://armaker.shawnlehner.com/api/v1/generate](https://armaker.shawnlehner.com/api/v1/generate). You can find out more about how to use the API via the documentation site referenced below.

If you want to include this generation in your app, you can use the *MarkerGenerator* class directly. It is designed to be stand alone and has fairly minimal requirements so you should be able to include it with most versions of .NET.
# Documentation
To see this tool in action and documentation on how to utilize it, please visit the documentation at [https://shawnlehner.github.io/ARMaker/](https://shawnlehner.github.io/ARMaker/)
# Background
I am relatively new to augmented reality but one issue I observed immediately was the lack of a good resource for generating highly trackable and uniform markers. For example, Vuforia provides a great sample image but as soon as you need to expand to multiple markers, you are left tracking down (oh the puns) your own set of markers. In addition, I wanted to be able to provide a uniform look to the markers without needed to generate each one by hand. This need is what led me to create the above utility.
