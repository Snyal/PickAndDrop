function drawBoundingBoxes(img, boundingBoxes, imgWidth, imageHeight) {
    var canvas = document.getElementById('canvasYoloTest');
    var context = canvas.getContext('2d');

    
    canvas.width = imgWidth;
    canvas.height = imageHeight;
    drawImageScaled(img, context, imgWidth, imageHeight);

    for (const [, value] of Object.entries(boundingBoxes)) {
        var topX = (value.x) - value.width / 2;
        var topY = (value.y) - value.height / 2;
        context.beginPath();
        context.rect(topX, topY, value.width, value.height);
        context.lineWidth = 10;
        context.strokeStyle = 'black';
        context.stroke();
    }
    
}

function drawImageScaled(img, ctx, imgWidth, imageHeight) {
    var canvas = ctx.canvas;
    var hRatio = canvas.width / imgWidth;
    var wRatio = canvas.height / imageHeight;
    var ratio = Math.min(hRatio, wRatio);
    var centerShift_x = (canvas.width - imgWidth * ratio) / 2;
    var centerShift_y = (canvas.height - imageHeight * ratio) / 2;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, 0, 0, imgWidth, imageHeight, centerShift_x, centerShift_y, imgWidth * ratio, imageHeight * ratio);


    img.style.width = 10 + "vw";
    canvas.style.width = 10 + "vw";

}
async function detect(event) {
    event.preventDefault(); // Prevent the form from submitting the traditional way

    const fileInput = document.getElementById('image');
    const file = fileInput.files[0];

    const reader = new FileReader();

    // Validate the file type
    if (!file) {
        return;
    }

    const fileType = file.type;
    if (fileType !== "image/jpeg" && fileType !== "image/jpg") {
        return;
    }

    const formData = new FormData();
    formData.append("image", file);

    try {
        const response = await fetch('http://localhost:5000/yoloDetection/detect', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            throw new Error('Network response was not ok ' + response.statusText);
        }

        const result = await response.json();

        return new Promise((resolve) => {
            reader.readAsDataURL(file);
            reader.onload = function (e) {
                preview.setAttribute('src', e.target.result);

                const img = new Image();
                img.src = e.target.result;

                img.onload = async function () {
                    const width = img.width;
                    const height = img.height;

                    drawBoundingBoxes(preview, result, width, height);
                    resolve({
                        imgWidth: width,
                        imgHeight: height,
                        bb: result
                    });
                };
            };
        });
    } catch (error) {
        console.error('There was a problem with the fetch operation:', error);
    }
}
async function initModel(modelName) {
    try {

        console.log(modelName)
        const response = await fetch('http://localhost:5000/yoloDetection/initModel', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(modelName)
        });

        if (!response.ok) {
            throw new Error('The new model can\'t be init ' + response.statusText);
        }

        
    } catch (error) {
        console.error('There was a problem with the fetch operation:', error);
    }
}