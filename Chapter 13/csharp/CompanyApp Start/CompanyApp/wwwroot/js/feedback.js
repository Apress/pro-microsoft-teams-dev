window.addEventListener("DOMContentLoaded", function () {

    // Different query options:
    navigator.permissions.query({ name: 'camera' });
    navigator.permissions.query({ name: 'microphone' });


    // Grab elements, create settings, etc.
    var canvas = document.getElementById('canvas');
    var context = canvas.getContext('2d');
    var video = document.getElementById('video');

    // Put video listeners into place
    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        navigator.mediaDevices.getUserMedia({ audio: false, video: true }).then(function (stream) {
            //video.src = window.URL.createObjectURL(stream);
            video.srcObject = stream;
            video.play();
        }).catch(error => {

            console.log(error);
        });
    }

    // Trigger photo take
    document.getElementById('snap').addEventListener('click', function () {
        context.drawImage(video, 0, 0, 640, 480);
    });

}, false);
