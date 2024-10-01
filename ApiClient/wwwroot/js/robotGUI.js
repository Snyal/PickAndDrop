// Setup scene, camera, renderer
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, window.innerWidth * 0.4 / window.innerHeight, 0.1, 1000);
const renderer = new THREE.WebGLRenderer();
document.getElementById('right').appendChild(renderer.domElement);
renderer.setClearColor(0xffffff, 1);

const light = new THREE.DirectionalLight(0xffffff, 1);
light.position.set(0, 15, 0).normalize();
scene.add(light);

const plight = new THREE.PointLight(0xffffff, 1, 100);
plight.position.set(0, 20, 10);
scene.add(plight);

const size = 50;
const divisions = 10;
const gridHelper = new THREE.GridHelper(size, divisions);
scene.add(gridHelper);

// Position the camera
camera.position.set(10, 18, 18);
camera.lookAt(8, 10, 0);

// Function to adjust the renderer size while keeping the aspect ratio
function resizeRenderer() {
    const width = window.innerWidth *0.4; // Half of the screen width
    const height = width * (window.innerHeight / window.innerWidth); // Maintain aspect ratio
    renderer.setSize(width, height); // Set the new renderer size
    camera.aspect = width / height; // Update the camera aspect ratio
    camera.updateProjectionMatrix(); // Apply the changes to the camera
    renderer.render(scene, camera); // Re-render the scene
}

// Call resizeRenderer initially to set correct size
resizeRenderer();

// Add event listener to adjust size dynamically on window resize
window.addEventListener('resize', resizeRenderer);

renderer.render(scene, camera);


// ROBOT
joints = [];
links = [];
targetPosition = [];
anglesFreedom = [];
targets = [];

targetsObj = [];

const dropZonePosition = [5, 3, 9];

// Materials for the joints and links
const materialJoint = new THREE.MeshPhongMaterial({ color: 0xFF0000 }); 
const materialLink = new THREE.MeshPhongMaterial({ color: 0xcccccc }); 

// Geometry for cylindrical links and joints
const jointGeometry = new THREE.SphereGeometry(0.5, 16, 16); 
const linkGeometry = new THREE.BoxGeometry(1, 1, 0.4); 

const animationTime = 600;

// set default value
initRobot()

async function initRobot() {
    const selectElement = document.getElementById('robot-select');
    const selectedOption = selectElement.options[selectElement.selectedIndex];
    const robotName = selectedOption.value
    const configuration = selectedOption.getAttribute('data-configuration');
    anglesFreedom = JSON.parse(selectedOption.getAttribute('data-anglefreedom'));

    if (configuration) {
        const configArray = JSON.parse(configuration);
 
        try {
            const response = await fetch('http://localhost:5000/robot/initRobot', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(robotName)
            });

            if (!response.ok) {
                throw new Error('The robot can\'t be init ' + response.statusText);
            }

            updateRobotGUI(configArray);
        } catch (error) {
            console.error('There was a problem with the fetch operation:', error);
        }

    }
}

function updateRobotGUI(robotSetup) {
    if (joints.length > 0) {
        scene.remove(joints[0]);
    }
    //scene.removeObject3D(links[0]);

    joints = [];
    links = [];

    function linkBetweenJoin(startJoint, direction) {

        const link = new THREE.Mesh(linkGeometry, materialLink);

        // Set the position of the link to be the midpoint
        final_position = [0, 0, 0];
        for (let i = 0; i < direction.length; i++) {
            if (direction[i] != 0) {
                final_position[i] = direction[i] / 2;
            }
        }
        link.position.set(final_position[0], final_position[1], final_position[2]);

        const length = Math.sqrt(direction[0] ** 2 + direction[1] ** 2 + direction[2] ** 2);
        link.scale.set(1, length, 1);

        // Set the rotation based on the direction
        const directionVector = new THREE.Vector3(direction[0], direction[1], direction[2]);
        directionVector.normalize(); // Normalize to get direction

        // Create a default up vector (the initial orientation of the link is along the Y-axis)
        const defaultUp = new THREE.Vector3(0, 1, 0);

        // Calculate the quaternion that rotates defaultUp to directionVector
        const quaternion = new THREE.Quaternion().setFromUnitVectors(defaultUp, directionVector);

        // Apply the rotation to the link
        link.setRotationFromQuaternion(quaternion);

        startJoint.add(link);
        links.push(link);
    }

    currentPosition_x = 0;
    currentPosition_y = 0;
    currentPosition_z = 0;

    // Create the base root
    const root = new THREE.Mesh(jointGeometry, materialJoint);
    root.position.set(currentPosition_x, currentPosition_y, currentPosition_z);
    scene.add(root);
    joints.push(root);


    // Create the joints based on configuration
    for (let i = 0; i < robotSetup.length; i++) {
        const joint = new THREE.Mesh(jointGeometry, materialJoint);

        joint.position.set(robotSetup[i][0], robotSetup[i][1], robotSetup[i][2]);
        joints[i].add(joint);

        // Create a link between the previous joint and the current joint
        linkBetweenJoin(joints[i], robotSetup[i]);

        joints.push(joint);
    }

    joints[0].rotation.y = 0.5;

    renderer.render(scene, camera);
    //animate();
}

async function moveTo(x, y, z, a, b, c, duration) {

    try {
        const response = await fetch('http://localhost:5000/robot/moveTo', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                x: x,
                y: y,
                z: z,
                a: a,
                b: b,
                c: c
            })
        });

        if (!response.ok) {
            throw new Error('The robot can\'t be init ' + response.statusText);
        } else {

        }

        targetPosition = await response.json();
        rotateTo(duration);

    } catch (error) {
        console.error('There was a problem with the fetch operation:', error);
    }

}

function rotateTo(duration) {
    // Store the starting rotations
    const startRotations = [
        joints[0].rotation.y,
        joints[1].rotation.z,
        joints[2].rotation.z,
        joints[3].rotation.x,
        joints[4].rotation.x,
        joints[5].rotation.x,
    ];

    const startTime = performance.now();

    function animate() {
        const elapsed = performance.now() - startTime;
        const progress = Math.min(elapsed / duration, 1); // Clamp progress to [0, 1]

        for (let i = 0; i < joints.length; i++) {
            let newValue = startRotations[i] + (targetPosition[i] - startRotations[i]) * progress;
            joints[i].rotation.x = (anglesFreedom[i][0] * newValue);
            joints[i].rotation.y = (anglesFreedom[i][1] * newValue);
            joints[i].rotation.z = (anglesFreedom[i][2] * newValue);
        }


        // Render the scene
        renderer.render(scene, camera);

        if (progress < 1) {
            requestAnimationFrame(animate); // Continue animation
        }
    }

    animate();

}

async function launchPickAndDrop() {
  
    let offsetDropZone = 0.4;

    for (let i = 0; i < targets.length; i++) {

        currentOffset = offsetDropZone * i;
        await moveTo(targets[i][0], 3, targets[i][1], 0, 0, -2, animationTime)
        await new Promise(resolve => setTimeout(resolve, animationTime * 2));
        pickZone.remove(targetsObj[i]);

        await moveTo(8, 6, 2, 0, 0, 0, animationTime/2);
        await new Promise(resolve => setTimeout(resolve, animationTime / 2));

        await moveTo(dropZonePosition[0] + currentOffset, dropZonePosition[1] + 1, dropZonePosition[2], 0, 0, -2, animationTime / 2);
        await new Promise(resolve => setTimeout(resolve, animationTime));
        dropZone.add(targetsObj[i])
        targetsObj[i].position.x = currentOffset;
        targetsObj[i].position.y = 1;
        targetsObj[i].position.z = 0;
      

        await moveTo(8, 6, 2, 0, 0, 0, animationTime / 2);
        await new Promise(resolve => setTimeout(resolve, animationTime / 2));

    }

    // go to base position
    await new Promise(resolve => setTimeout(resolve, animationTime));
    await moveTo(5, 10, 0, 0, 0, 0, animationTime*2)

}

async function generateTargetBaseOnImage(event) {
    const result = await detect(event)

    if (typeof pickZone !== 'undefined') {
        scene.remove(pickZone);
        scene.remove(dropZone);
    }

    var pickZoneWidth = 5;
    var pickZoneHeight = pickZoneWidth * (result.imgHeight / result.imgWidth)

    // drop zone
    const dropZoneGeometry = new THREE.BoxGeometry(20, 2, 2);
    const dropZoneMaterial = new THREE.MeshPhongMaterial({ color: 0x00A5E3 });
    dropZone = new THREE.Mesh(dropZoneGeometry, dropZoneMaterial);
    dropZone.position.x = dropZonePosition[0];
    dropZone.position.y = dropZonePosition[1];
    dropZone.position.z = dropZonePosition[2];

    scene.add(dropZone);

    // pick zone
    const pickZoneGeometry = new THREE.BoxGeometry(pickZoneWidth, 2, pickZoneHeight);
    const pickZoneMaterial = new THREE.MeshPhongMaterial({ color: 0xFF5C77});
    pickZone = new THREE.Mesh(pickZoneGeometry, pickZoneMaterial);
   
    pickZone.position.x = 15;
    pickZone.position.y = 2;
    pickZone.position.z = 0;

    scene.add(pickZone);

    const objGeometry = new THREE.TorusGeometry(0.3, 0.3, 16, 100);
    const objMaterial = new THREE.MeshPhongMaterial({ color: 0xE7C582 });

    targets = []
    targetsObj = []
    
 
    for (let i = 0; i < result.bb.length; i++) {
        var xCenterObj = result.bb[i].x * pickZoneWidth / result.imgWidth;
        var zCenterObj = result.bb[i].y * pickZoneHeight / result.imgHeight;

        obj = new THREE.Mesh(objGeometry, objMaterial);

        obj.position.x = xCenterObj - pickZoneWidth/2;
        obj.position.y = 1;
        obj.position.z = zCenterObj - pickZoneHeight / 2;

        obj.rotation.x = Math.PI/2;
        pickZone.add(obj);

        const worldPosition = new THREE.Vector3();  // Define a new Vector3 for world position
        obj.getWorldPosition(worldPosition);  // Get the world position

        targetsObj.push(obj);
        targets.push([worldPosition.x, worldPosition.z]);
    }

    renderer.render(scene, camera);
}