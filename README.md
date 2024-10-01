![alt Text](https://github.com/Snyal/PickAndDrop/blob/main/demo.gif)

# Pick and Drop Application with a Robotic Arm

## Description

This project is a small application that allows a robotic arm to perform **pick and drop** operations. A neural network (here **YOLOv8**) detects the position of objects in an image, and then a robot uses **inverse kinematics** to grab and move the object.

This is a very naive implementation, only intended to illustrate the concept.

## Features

- Real-time object detection with `YOLOv8`
- Control of a robotic arm to pick and move objects
- Use of inverse kinematics for robot positioning ([Convert `JS` from this repo](https://github.com/glumb/kinematics/blob/master/src/kinematics.js) to `C#`)

## Prerequisites
- .net8
- windows

## Installation
![alt text](https://github.com/Snyal/PickAndDrop/blob/main/projectArchitecture.png?raw=true)

* Clone the repository
* Launch all services :
   * dotnet run --project ApiGateway --urls http://localhost:5000
   * dotnet run --project RobotMicroservice --urls http://localhost:5001
   * dotnet run --project YoloMicroservice --urls http://localhost:5002
 * Run Client
