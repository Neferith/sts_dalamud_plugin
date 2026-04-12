function attachCopyButtons(root) {
    root.querySelectorAll('pre:not(.sts-copy-attached)').forEach(pre => {
        pre.classList.add('sts-copy-attached');
        const content = pre.innerText.trim(); // lu AVANT d'ajouter le bouton
        const btn = document.createElement('button');
        btn.textContent = 'Copier';
        btn.className = 'sts-copy-btn';
        btn.addEventListener('click', () => {
            navigator.clipboard.writeText('```\n' + content + '\n```').then(() => {
                btn.textContent = 'Copié !';
                setTimeout(() => btn.textContent = 'Copier', 2000);
            });
        });
        pre.style.position = 'relative';
        pre.appendChild(btn);
    });
}

const observer = new MutationObserver(() => attachCopyButtons(document.body));
observer.observe(document.body, { childList: true, subtree: true });

// Au cas où du contenu est déjà présent
attachCopyButtons(document.body);
