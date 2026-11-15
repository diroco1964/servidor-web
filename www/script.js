document.addEventListener("DOMContentLoaded", function() {
    const button = document.getElementById("myButton");
    button.addEventListener("click", function() {
        alert("¡Hola! Has hecho clic en el botón.");
    });

    const resultado = document.getElementById("resultado");

    // Muestra en pantalla lo que respondió el servidor.
    async function mostrar(metodo, respuesta) {
        const texto = await respuesta.text();
        resultado.textContent =
            metodo + " -> " + respuesta.status + " " + respuesta.statusText + "\n" +
            "Content-Type: " + respuesta.headers.get("Content-Type") + "\n\n" +
            texto;
    }

    async function enviar(metodo, url, opciones) {
        resultado.textContent = "Enviando " + metodo + "...";
        try {
            const respuesta = await fetch(url, opciones);
            await mostrar(metodo, respuesta);
        } catch (error) {
            resultado.textContent = metodo + " falló: " + error.message;
        }
    }

    // GET con parametros en la URL: el servidor los registra en el log como "query".
    document.getElementById("btnGet").addEventListener("click", function() {
        const parametros = new URLSearchParams({
            origen: "boton",
            hora: new Date().toLocaleTimeString()
        });
        enviar("GET", "/?" + parametros.toString(), { method: "GET" });
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
        });
    });
});
