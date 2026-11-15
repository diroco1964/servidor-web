document.addEventListener("DOMContentLoaded", function() {
    const button = document.getElementById("myButton");
    button.addEventListener("click", function() {
        alert("¡Hola! Has hecho clic en el botón.");
    });

    const resultado = document.getElementById("resultado");

    // Solo muestra un mensaje corto; la página no cambia ni se abre otra.
    async function enviar(metodo, url, opciones, mensajeOk) {
        resultado.textContent = "Enviando " + metodo + "...";
        try {
            const respuesta = await fetch(url, opciones);
            if (respuesta.ok) {
                resultado.textContent = mensajeOk;
            } else {
                resultado.textContent = "El servidor respondió con error " + respuesta.status + ".";
            }
        } catch (error) {
            resultado.textContent = "No se pudo conectar con el servidor.";
        }
    }

    // GET con parametros en la URL: el servidor los registra en el log como "query".
    document.getElementById("btnGet").addEventListener("click", function() {
        const parametros = new URLSearchParams({
            origen: "boton",
            hora: new Date().toLocaleTimeString()
        });
        enviar("GET", "/?" + parametros.toString(), { method: "GET" }, "Datos recibidos (GET).");
    });

    // POST con cuerpo: el servidor lo registra en el log como "body".
    document.getElementById("btnPost").addEventListener("click", function() {
        const datos = new URLSearchParams({
            nombre: "prueba",
            mensaje: "Hola desde el boton POST",
            hora: new Date().toLocaleTimeString()
        });
        enviar("POST", "/", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: datos.toString()
        }, "Datos recibidos (POST).");
    });
});
