(() => {
    "use strict";

    function announce(message, assertive = false) {
        let region = document.getElementById("globalLiveRegion");
        if (!region) {
            region = document.createElement("div");
            region.id = "globalLiveRegion";
            region.className = "visually-hidden";
            region.setAttribute("role", assertive ? "alert" : "status");
            region.setAttribute("aria-live", assertive ? "assertive" : "polite");
            document.body.appendChild(region);
        }

        region.textContent = "";
        window.setTimeout(() => {
            region.textContent = message;
        }, 25);
    }

    async function loadCategories(select) {
        if (!select || select.dataset.loaded === "true") {
            return;
        }

        const response = await fetch("/api/attributes/categories", {
            headers: { Accept: "application/json" }
        });
        if (!response.ok) {
            throw new Error("Unable to load attribute categories.");
        }

        const categories = await response.json();
        for (const category of categories) {
            const option = document.createElement("option");
            option.value = category.id;
            option.textContent = category.name;
            select.appendChild(option);
        }
        select.dataset.loaded = "true";
    }

    function initAttributeLookup(select) {
        if (!select || typeof window.TomSelect !== "function" || select.tomselect) {
            return null;
        }

        const categorySelect = select.dataset.categorySelector
            ? document.querySelector(select.dataset.categorySelector)
            : null;
        const recentToggle = select.dataset.recentSelector
            ? document.querySelector(select.dataset.recentSelector)
            : null;

        const control = new window.TomSelect(select, {
            valueField: "id",
            labelField: "name",
            searchField: ["name", "category"],
            create: false,
            maxItems: 1,
            maxOptions: 50,
            preload: "focus",
            loadThrottle: 300,
            shouldLoad: () => true,
            load(query, callback) {
                const parameters = new URLSearchParams({
                    prefix: query,
                    recent: recentToggle?.checked ? "true" : "false",
                    limit: "50"
                });
                if (categorySelect?.value) {
                    parameters.set("categoryId", categorySelect.value);
                }
                if (select.dataset.profileId) {
                    parameters.set("profileId", select.dataset.profileId);
                }

                fetch(`/api/attributes?${parameters}`, { headers: { Accept: "application/json" } })
                    .then(response => {
                        if (!response.ok) {
                            throw new Error("Unable to load attributes.");
                        }
                        return response.json();
                    })
                    .then(callback)
                    .catch(() => {
                        announce("Attribute lookup failed. Try again.", true);
                        callback();
                    });
            },
            render: {
                option(item, escape) {
                    const builtIn = item.isBuiltIn
                        ? '<span class="badge text-bg-primary ms-2">Built-in</span>'
                        : "";
                    return `<div><span class="fw-semibold">${escape(item.name)}</span>${builtIn}<div class="small text-muted">${escape(item.category)}</div></div>`;
                },
                item(item, escape) {
                    return `<div>${escape(item.name)} <span class="text-muted">(${escape(item.category)})</span></div>`;
                },
                no_results() {
                    return '<div class="no-results p-2 text-muted">No matching attributes</div>';
                }
            }
        });

        const reload = () => {
            control.clearOptions();
            control.load(control.control_input.value || "");
        };
        categorySelect?.addEventListener("change", reload);
        recentToggle?.addEventListener("change", reload);
        return control;
    }

    function initTagAutocomplete(input) {
        if (!input || typeof window.TomSelect !== "function" || input.tomselect) {
            return null;
        }

        return new window.TomSelect(input, {
            valueField: "value",
            labelField: "text",
            searchField: "text",
            delimiter: ",",
            create: input.dataset.allowCreate !== "false",
            persist: false,
            plugins: ["remove_button"],
            maxOptions: 30,
            loadThrottle: 300,
            load(query, callback) {
                const parameters = new URLSearchParams({ prefix: query, limit: "30" });
                fetch(`/api/tags?${parameters}`, { headers: { Accept: "application/json" } })
                    .then(response => response.ok ? response.json() : Promise.reject())
                    .then(callback)
                    .catch(() => {
                        announce("Tag suggestions are temporarily unavailable.", true);
                        callback();
                    });
            }
        });
    }

    function initCloudImageUpload(dropzone) {
        if (!dropzone || dropzone.dataset.initialized === "true") {
            return;
        }

        const target = document.querySelector(dropzone.dataset.target || "");
        const fileInput = dropzone.querySelector("input[type='file']");
        const status = dropzone.querySelector("[data-upload-status]");
        const preview = dropzone.querySelector("[data-upload-preview]");
        const cloudName = dropzone.dataset.cloudName || "";
        const uploadPreset = dropzone.dataset.uploadPreset || "";
        const submitButton = dropzone.closest("form")?.querySelector("[type='submit']");
        const maxBytes = 10 * 1024 * 1024;

        if (!target || !fileInput) {
            return;
        }

        dropzone.dataset.initialized = "true";

        const setStatus = (message, isError = false) => {
            if (!status) return;
            status.textContent = message;
            status.classList.toggle("text-danger", isError);
            status.classList.toggle("text-muted", !isError);
        };

        const setBusy = busy => {
            dropzone.classList.toggle("is-uploading", busy);
            dropzone.setAttribute("aria-busy", busy ? "true" : "false");
            fileInput.disabled = busy;
            if (submitButton) submitButton.disabled = busy;
            const attributeRow = dropzone.closest("[data-attribute-row]");
            if (attributeRow) attributeRow.dataset.uploadBusy = busy ? "true" : "false";
            dropzone.dispatchEvent(new CustomEvent("cloud-upload-state", {
                bubbles: true,
                detail: { busy }
            }));
        };

        const updatePreview = url => {
            if (!preview) return;
            if (url) {
                preview.src = url;
                preview.classList.remove("d-none");
            } else {
                preview.removeAttribute("src");
                preview.classList.add("d-none");
            }
        };

        const upload = async file => {
            if (!file?.type.startsWith("image/")) {
                setStatus("Choose an image file.", true);
                announce("Choose an image file.", true);
                return;
            }
            if (file.size > maxBytes) {
                setStatus("The image must be smaller than 10 MB.", true);
                announce("The image must be smaller than 10 MB.", true);
                return;
            }
            if (!cloudName || !uploadPreset) {
                setStatus("Cloud upload is not configured. Enter an HTTPS image URL instead.", true);
                announce("Cloud image upload is not configured.", true);
                return;
            }

            setBusy(true);
            setStatus("Uploading image…");
            const data = new FormData();
            data.append("file", file);
            data.append("upload_preset", uploadPreset);

            try {
                const response = await fetch(
                    `https://api.cloudinary.com/v1_1/${encodeURIComponent(cloudName)}/image/upload`,
                    { method: "POST", body: data });
                const payload = await response.json().catch(() => null);
                if (!response.ok || !payload?.secure_url) {
                    throw new Error(payload?.error?.message || "The upload service rejected the image.");
                }

                target.value = payload.secure_url;
                target.dispatchEvent(new Event("change", { bubbles: true }));
                updatePreview(payload.secure_url);
                setStatus("Image uploaded. Save the form to keep this value.");
                announce("Image uploaded successfully.");
            } catch (error) {
                const message = error instanceof Error ? error.message : "Image upload failed.";
                setStatus(message, true);
                announce(message, true);
            } finally {
                setBusy(false);
                fileInput.value = "";
            }
        };

        dropzone.addEventListener("click", event => {
            if (event.target !== fileInput) fileInput.click();
        });
        dropzone.addEventListener("keydown", event => {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                fileInput.click();
            }
        });
        ["dragenter", "dragover"].forEach(name => dropzone.addEventListener(name, event => {
            event.preventDefault();
            dropzone.classList.add("is-dragging");
        }));
        ["dragleave", "drop"].forEach(name => dropzone.addEventListener(name, event => {
            event.preventDefault();
            dropzone.classList.remove("is-dragging");
        }));
        dropzone.addEventListener("drop", event => upload(event.dataTransfer?.files?.[0]));
        fileInput.addEventListener("change", () => upload(fileInput.files?.[0]));
        target.addEventListener("change", () => updatePreview(target.value.trim()));
        updatePreview(target.value.trim());
    }

    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll("[data-attribute-lookup]").forEach(select => {
            const categorySelector = select.dataset.categorySelector;
            const categorySelect = categorySelector ? document.querySelector(categorySelector) : null;
            loadCategories(categorySelect)
                .then(() => initAttributeLookup(select))
                .catch(() => announce("Attribute categories could not be loaded.", true));
        });

        document.querySelectorAll("[data-tag-autocomplete]").forEach(initTagAutocomplete);
        document.querySelectorAll("[data-cloud-image-upload]").forEach(initCloudImageUpload);
    });

    window.cvUi = { announce, initAttributeLookup, initTagAutocomplete, initCloudImageUpload };
})();
