mergeInto(LibraryManager.library, {
    SaveFile: function(data, length, filename) {
        try {
            // Создаем view напрямую из памяти
            var bytes = new Uint8Array(HEAPU8.buffer, data, length);
            
            // Проверка первых 4 байт (должны быть 'RIFF')
            if (bytes[0] !== 0x52 || bytes[1] !== 0x49 || bytes[2] !== 0x46 || bytes[3] !== 0x46) {
                console.error('Invalid WAV header:', bytes.subarray(0,4));
                return;
            }

            var blob = new Blob([bytes], {type: 'audio/wav'});
            var url = URL.createObjectURL(blob);
            
            var link = document.createElement('a');
            link.href = url;
            link.download = UTF8ToString(filename);
            link.style.display = 'none';
            
            document.body.appendChild(link);
            setTimeout(function() {
                link.click();
                URL.revokeObjectURL(url);
                document.body.removeChild(link);
            }, 100);
        } catch(e) {
            console.error('File save error:', e);
        }
    }
});