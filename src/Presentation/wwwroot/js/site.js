document.addEventListener("DOMContentLoaded", () => {
  const aboutDisplay = document.querySelector("[data-profile-about-open]");
  const aboutForm = document.querySelector("[data-profile-about-form]");
  const aboutCancel = document.querySelector("[data-profile-about-cancel]");

  if (aboutDisplay && aboutForm && aboutCancel) {
	aboutDisplay.addEventListener("click", () => {
	  aboutDisplay.hidden = true;
	  aboutForm.hidden = false;
	  aboutForm.querySelector("textarea")?.focus();
	});

	aboutCancel.addEventListener("click", () => {
	  aboutForm.hidden = true;
	  aboutDisplay.hidden = false;
	});
  }

  const openEditButton = document.querySelector("[data-profile-edit-open]");
  const cancelEditButton = document.querySelector("[data-profile-edit-cancel]");
  const detailsDisplay = document.querySelector("[data-profile-details-display]");
  const detailsForm = document.querySelector("[data-profile-details-form]");

  if (openEditButton && cancelEditButton && detailsDisplay && detailsForm) {
	openEditButton.addEventListener("click", () => {
	  detailsDisplay.hidden = true;
	  detailsForm.hidden = false;
	  openEditButton.hidden = true;
	  detailsForm.querySelector("input")?.focus();
	});

	cancelEditButton.addEventListener("click", () => {
	  detailsForm.hidden = true;
	  detailsDisplay.hidden = false;
	  openEditButton.hidden = false;
	});
  }

  const avatarInput = document.querySelector("[data-avatar-input]");
  if (avatarInput) {
	avatarInput.addEventListener("change", () => {
	  const form = avatarInput.closest("form");
	  if (form && avatarInput.files && avatarInput.files.length > 0) {
		form.submit();
	  }
	});
  }

  const locationDisplay = document.querySelector("[data-profile-location-open]");
  const locationForm = document.querySelector("[data-profile-location-form]");
  const locationCancel = document.querySelector("[data-profile-location-cancel]");

  if (locationDisplay && locationForm && locationCancel) {
	locationDisplay.addEventListener("click", () => {
	  locationDisplay.hidden = true;
	  locationForm.hidden = false;
	  locationForm.querySelector("input")?.focus();
	});

	locationCancel.addEventListener("click", () => {
	  locationForm.hidden = true;
	  locationDisplay.hidden = false;
	});
  }

  const documentDropzone = document.querySelector("[data-document-dropzone]");
  const documentInput = document.querySelector("[data-document-input]");
  const documentFileName = document.querySelector("[data-document-file-name]");

  if (documentDropzone && documentInput && documentFileName) {
	const setFileName = () => {
	  const file = documentInput.files && documentInput.files.length > 0 ? documentInput.files[0] : null;
	  documentFileName.textContent = file ? file.name : "Файл ще не обрано";
	  documentDropzone.classList.toggle("document-upload--has-file", Boolean(file));
	};

	documentDropzone.addEventListener("click", (event) => {
	  if (event.target === documentInput) {
	    return;
	  }

	  documentInput.click();
	});

	documentInput.addEventListener("change", setFileName);

	documentDropzone.addEventListener("dragenter", (event) => {
	  event.preventDefault();
	  documentDropzone.classList.add("is-dragover");
	});

	documentDropzone.addEventListener("dragover", (event) => {
	  event.preventDefault();
	  documentDropzone.classList.add("is-dragover");
	});

	documentDropzone.addEventListener("dragleave", (event) => {
	  event.preventDefault();
	  if (!documentDropzone.contains(event.relatedTarget)) {
	    documentDropzone.classList.remove("is-dragover");
	  }
	});

	documentDropzone.addEventListener("drop", (event) => {
	  event.preventDefault();
	  documentDropzone.classList.remove("is-dragover");

	  if (event.dataTransfer && event.dataTransfer.files.length > 0) {
	    const dataTransfer = new DataTransfer();
	    dataTransfer.items.add(event.dataTransfer.files[0]);
	    documentInput.files = dataTransfer.files;
	    setFileName();
	  }
	});

	setFileName();
  }
});
