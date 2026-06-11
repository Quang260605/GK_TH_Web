// Global toast notification system
function showNotification(message, type = 'success') {
    let container = document.getElementById('toast-container-custom');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container-custom';
        container.className = 'toast-container-custom';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast-custom toast-custom-${type} animate-fade-in`;
    
    let iconHtml = '';
    if (type === 'success') {
        iconHtml = '<i class="fa-solid fa-circle-check"></i>';
    } else if (type === 'danger') {
        iconHtml = '<i class="fa-solid fa-circle-exclamation"></i>';
    } else {
        iconHtml = '<i class="fa-solid fa-circle-info"></i>';
    }

    toast.innerHTML = `
        <div class="toast-custom-icon">${iconHtml}</div>
        <div class="toast-custom-body">${message}</div>
        <button type="button" class="toast-custom-close" onclick="this.parentElement.remove()"><i class="fa-solid fa-xmark"></i></button>
    `;

    container.appendChild(toast);
    
    // Trigger animation slide-in
    setTimeout(() => {
        toast.classList.add('show');
    }, 10);

    // Auto remove toast after 4 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(120%)';
        
        // Remove from DOM after transition finishes
        toast.addEventListener('transitionend', () => {
            toast.remove();
        });
    }, 4000);
}
