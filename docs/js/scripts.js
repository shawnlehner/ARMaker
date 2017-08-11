var maker = function() {

    var markerImage = null;
    var markerLoader = null;
    var markerDownloadButton = null;

    var currentId = null;
    var apiUrl = 'https://armaker.shawnlehner.com/api/v1/';

    initialize();

    return {

    };

    function initialize() {
        markerImage = document.getElementsByClassName('marker-image')[0];
        markerLoader = document.getElementsByClassName('marker-loader')[0];
        markerDownloadButton = document.getElementById('download-button');

        markerImage.addEventListener('load', function(e) {
            if (markerImage.src.indexOf('spacer.gif') === -1) {
                markerLoader.style.display = 'none';
            }
        });

        document.getElementById('generate-button').addEventListener('click', generateNewMarker);

        var typeOptions = document.querySelectorAll('input[name="artype"]');
        for(var i = 0; i < typeOptions.length; i++) {
            typeOptions[i].addEventListener('change', function() {
                if (this.checked) {
                    generateNewMarker();
                }
            });
        }
        

        generateNewMarker();
    }

    function generateNewMarker() {
        requestNewMarkerSeed(function(seed) {
            var type = document.querySelector('input[name="artype"]:checked').value;
            var markerUrlBase = apiUrl + 'generate?id=' + seed + '&type=' + type + '&size=';
            var previewImageUrl = markerUrlBase + markerImage.offsetWidth;
            var fullImageUrl = markerUrlBase + 1024 + '&download=true';

            // Update our control parameters
            markerImage.src = previewImageUrl;
            markerDownloadButton.href = fullImageUrl;
        });
    }

    function requestNewMarkerSeed(callback)
    {
        var xr = (window.XMLHttpRequest) ? new XMLHttpRequest() : new ActiveXObject('Microsoft.XMLHTTP');
        xr.onreadystatechange = function() {
            if (xr.readyState == 4 && xr.status == 200) {
                callback(parseInt(xr.responseText));
            }
        };

        xr.open('POST', apiUrl + 'id', true);
        xr.send();
    }
}();