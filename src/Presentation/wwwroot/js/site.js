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

  const goalFilterForm = document.querySelector("[data-goal-filter]");
  if (goalFilterForm) {
	const goalRange = goalFilterForm.querySelector("[data-goal-range]");
	const goalInput = goalFilterForm.querySelector("[data-goal-input]");
	const goalValue = goalFilterForm.querySelector("[data-goal-value]");
	const goalMax = goalFilterForm.querySelector("[data-goal-max]");
	const currencyInputs = goalFilterForm.querySelectorAll("[data-goal-currency]");
	const filterInputs = goalFilterForm.querySelectorAll(".fundraisers-filter-chip__input");

	const maxByCurrency = {
	  ALL: 10000,
	  UAH: 10000,
	  USD: 5000,
	  EUR: 5000
	};

	const formatGoalValue = (value) => new Intl.NumberFormat("uk-UA").format(value);

	const setChipState = (input) => {
	  const chip = input.closest(".fundraisers-filter-chip");
	  if (!chip) {
		return;
	  }

	  chip.classList.toggle("fundraisers-filter-chip--active", input.checked);
	};

	const refreshChipStates = () => {
	  filterInputs.forEach((input) => setChipState(input));
	};

	const syncGoalUi = (value, maxValue) => {
	  const normalizedValue = Math.max(1, Math.min(value, maxValue));

	  goalRange.max = maxValue.toString();
	  goalInput.max = maxValue.toString();
	  goalRange.value = normalizedValue.toString();
	  goalInput.value = normalizedValue.toString();
	  goalValue.textContent = formatGoalValue(normalizedValue);
	  goalMax.textContent = formatGoalValue(maxValue);
	};

	const getSelectedCurrency = () => {
	  const selected = Array.from(currencyInputs).find((input) => input.checked);
	  return selected ? selected.value.toUpperCase() : "ALL";
	};

	if (goalRange && goalInput && goalValue && goalMax) {
	  const initialCurrency = getSelectedCurrency();
	  const initialMax = maxByCurrency[initialCurrency] ?? maxByCurrency.ALL;
	  const initialValue = Number.parseInt(goalInput.value || goalRange.value, 10) || initialMax;
	  syncGoalUi(initialValue, initialMax);

	  goalRange.addEventListener("input", () => {
		const maxValue = Number.parseInt(goalRange.max, 10) || maxByCurrency.ALL;
		syncGoalUi(Number.parseInt(goalRange.value, 10), maxValue);
	  });

	  goalInput.addEventListener("input", () => {
		const maxValue = Number.parseInt(goalInput.max, 10) || maxByCurrency.ALL;
		const nextValue = Number.parseInt(goalInput.value, 10) || 1;
		syncGoalUi(nextValue, maxValue);
	  });

	  currencyInputs.forEach((input) => {
		input.addEventListener("change", () => {
		  const currency = getSelectedCurrency();
		  const nextMax = maxByCurrency[currency] ?? maxByCurrency.ALL;
		  const currentValue = Number.parseInt(goalInput.value, 10) || nextMax;
		  syncGoalUi(currentValue, nextMax);
		});
	  });
	}

	filterInputs.forEach((input) => {
	  input.addEventListener("change", refreshChipStates);
	});

	refreshChipStates();
  }

  const recentDonationsFeed = document.querySelector("[data-recent-donations-feed]");
  if (recentDonationsFeed) {
	const recentDonationsTrack = recentDonationsFeed.querySelector("[data-recent-donations-track]");
	const recentDonationsPrev = recentDonationsFeed.querySelector("[data-recent-donations-prev]");
	const recentDonationsNext = recentDonationsFeed.querySelector("[data-recent-donations-next]");
	const recentDonationsUrl = recentDonationsFeed.getAttribute("data-feed-url");
	const refreshInterval = Number.parseInt(recentDonationsFeed.getAttribute("data-refresh-interval") || "12000", 10);

	if (recentDonationsTrack && recentDonationsUrl) {
	  let refreshInProgress = false;
	  let scrollStateTimeoutId = 0;

	  const getScrollStep = () => {
		const firstCard = recentDonationsTrack.querySelector(".recent-donation-card, .recent-donations-empty");
		if (!firstCard) {
		  return recentDonationsTrack.clientWidth;
		}

		const firstCardWidth = firstCard.getBoundingClientRect().width;
		const trackStyles = window.getComputedStyle(recentDonationsTrack);
		const gap = Number.parseFloat(trackStyles.columnGap || trackStyles.gap || "0") || 0;

		return firstCardWidth + gap;
	  };

	  const updateNavigationState = () => {
		if (!recentDonationsPrev || !recentDonationsNext) {
		  return;
		}

		const maxScrollLeft = Math.max(0, recentDonationsTrack.scrollWidth - recentDonationsTrack.clientWidth);
		const currentScrollLeft = recentDonationsTrack.scrollLeft;
		const canScroll = maxScrollLeft > 1;
		const canScrollLeft = canScroll && currentScrollLeft > 1;
		const canScrollRight = canScroll && currentScrollLeft < maxScrollLeft - 1;

		recentDonationsPrev.disabled = !canScrollLeft;
		recentDonationsNext.disabled = !canScrollRight;
	  };

	  const scrollRecentDonations = (direction) => {
		const step = Math.max(0, getScrollStep());
		if (step === 0) {
		  return;
		}

		recentDonationsTrack.scrollBy({
		  left: direction * step,
		  behavior: "smooth"
		});
	  };

	  recentDonationsPrev?.addEventListener("click", () => {
		scrollRecentDonations(-1);
	  });

	  recentDonationsNext?.addEventListener("click", () => {
		scrollRecentDonations(1);
	  });

	  recentDonationsTrack.addEventListener("scroll", () => {
		recentDonationsFeed.classList.add("recent-donations-feed--scrolling");
		if (scrollStateTimeoutId) {
		  window.clearTimeout(scrollStateTimeoutId);
		}

		scrollStateTimeoutId = window.setTimeout(() => {
		  recentDonationsFeed.classList.remove("recent-donations-feed--scrolling");
		}, 160);

		window.requestAnimationFrame(updateNavigationState);
	  });

	  window.addEventListener("resize", () => {
		window.requestAnimationFrame(updateNavigationState);
	  });

	  const refreshRecentDonations = async () => {
		if (refreshInProgress) {
		  return;
		}

		refreshInProgress = true;
		const previousScrollLeft = recentDonationsTrack.scrollLeft;

		try {
		  const response = await fetch(recentDonationsUrl, {
			headers: {
			  "X-Requested-With": "XMLHttpRequest"
			},
			cache: "no-store"
		  });

		  if (!response.ok) {
			updateNavigationState();
			return;
		  }

		  recentDonationsTrack.innerHTML = await response.text();
		  recentDonationsTrack.scrollLeft = previousScrollLeft;
		  recentDonationsFeed.classList.remove("recent-donations-feed--scrolling");
		  window.requestAnimationFrame(updateNavigationState);
		} catch (error) {
		  console.warn("Не вдалося оновити стрічку останніх донатів.", error);
		} finally {
		  refreshInProgress = false;
		}
	  };

	  updateNavigationState();
	  refreshRecentDonations();

	  if (Number.isFinite(refreshInterval) && refreshInterval > 0) {
		window.setInterval(refreshRecentDonations, refreshInterval);
	  }
	}
  }
});
