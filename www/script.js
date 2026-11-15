document.addEventListener("DOMContentLoaded", function() {
    const button = document.getElementById("myButton");
    button.addEventListener("click", function() {
        alert("¡Hola! Has hecho clic en el botón.");
    });

    const formulario = document.getElementById("formulario");
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

    // Lee lo que escribió el usuario; si falta algún campo, avisa y no envía.
    function leerDatos() {
        if (!formulario.reportValidity()) {
            return null;
        }
        return new URLSearchParams({
            nombre: document.getElementById("nombre").value.trim(),
            mensaje: document.getElementById("mensaje").value.trim()
        });
    }

    // GET: los datos viajan en la URL; el servidor los registra como "query".
    document.getElementById("btnGet").addEventListener("click", function() {
        const datos = leerDatos();
        if (datos === null) return;
        enviar("GET", "/?" + datos.toString(), { method: "GET" }, "Datos recibidos (GET).");
    });

    // POST: los datos viajan en el cuerpo; el servidor los registra como "body".
    document.getElementById("btnPost").addEventListener("click", function() {
        const datos = leerDatos();
        if (datos === null) return;
        enviar("POST", "/", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: datos.toString()
        }, "Datos recibidos (POST).");
    });
});
