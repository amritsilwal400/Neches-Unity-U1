# Neches River U1 - Unity Viewer

## Project Overview
Unity-based map viewer developed for the Neches River / LNVA research project at Lamar University.

The viewer uses Cesium for Unity to display river networks, USGS gauge locations, and selected pilot study areas.

## Current Features
- Top-down satellite map using Cesium for Unity
- 5,748 mapped river segments after removing in-lake features
- Four USGS gauge markers with interactive stage and discharge charts
- Historical gauge data with the 2011 drought period highlighted
- Three pilot areas: Rockland, Evadale, and LNVA canal

## Software Requirements
- Unity Editor 6000.0.85f1
- Cesium for Unity (configured through Unity Package Manager)
- Cesium ion account and an appropriate access token
- Internet connection for online terrain and imagery

## Opening the Project
1. Clone or download the repository.
2. Open Unity Hub.
3. Select Add project from disk.
4. Select the downloaded project folder.
5. Open the project using Unity 6000.0.85f1.
6. Configure your own Cesium ion access token.
7. Open U1_Pilots_Test from the Scenes folder.
8. Press Play to test the map and gauge charts.

## Project Structure
- Assets: Unity scenes, scripts, and project resources
- Packages: Package dependencies
- ProjectSettings: Unity project configuration

## Current Limitations
- Satellite imagery requires an internet connection.
- Users must configure their own Cesium ion token.
- Satellite water-mask comparison and validation are still in progress.

## Research
Lamar University
Mechanical Engineering

Developer: Amrit Silwal
Research Supervisor: Dr. Wenhao Yang
