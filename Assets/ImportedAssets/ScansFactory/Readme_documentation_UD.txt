Hello,
If you need much better texture resolution for HDRP, here you have it. We had to separate it from the package due to asset store submission requirements.

HIGH RESOLUTION TEKSTURES
https://bit.ly/3NxphzD

You need to replace the textures from the Assets\ScansFactory\UrbanDistrict\Common\Textures with the textures from the downloaded package

All rendering pipelines are in folders:

HDRP
Assets\ScansFactory\UrbanDistrict\HDRP

URP
Assets\ScansFactory\UrbanDistrict\URP

Builtin
Assets\ScansFactory\UrbanDistrict\BuiltIn

If the entire scene is pink, check if the correct rendering pipeline is installed in the project.
Also, make sure to set the correct rendering pipeline asset in both the Quality settings and the Graphics settings.
They can be found in the following folders:

HDRP
Assets\ScansFactory\UrbanDistrict\HDRP\Demo\HDRP_Settings\UrbanDistrict_01_Sky and Fog_override.asset

URP
Assets\ScansFactory\UrbanDistrict\URP\Demo_URP\SkyAndFog_URP\UrbanDistrict_01_Sky and Fog_override.asset

Builtin:
To use our post-processing in the Built-in Render Pipeline, you need to download the Post Processing package from the Package Manager.
To ensure the Built-in settings work correctly, we recommend starting a clean project with Unity's default Built-in settings.

If the tips of the plants appear silver, reduce the Intensity Multiplier under Lighting → Environment → Environment Reflections to 0.2

If shadows are not visible in Built-in, please set the Shadow Distance to 150 in Project Settings.

If the Skybox Material is not assigned, it can be found in the following folders:

URP
Assets\ScansFactory\UrbanDistrict\URP\Demo_URP\SkyAndFog_URP\Sky_01_URP\m_SkyMaterial_URP_01.mat

Builtin
Assets\ScansFactory\UrbanDistrict\BuiltIn\Demo_BuiltIn\SkyAndFog_BuiltIn\Sky_01_BuiltIn\m_SkyMaterial.mat

ScansFactoryTeam