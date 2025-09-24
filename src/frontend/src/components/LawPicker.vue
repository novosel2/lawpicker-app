<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { languages, lawTypes } from '../store/lists';
import axios from '../axios-config.ts';
import DOMPurify from 'dompurify';
import JSZip from 'jszip';
import { saveAs } from 'file-saver';

// Types
interface LawDocument {
    id: string;
    celex: string;
    type: string;
    date: string;
    title: string;
}

interface ApiResponse {
    count: number;
    documents: LawDocument[];
}

interface PdfResponse {
    celex: string;
    url: string | null;
    problem: string | null;
    availableLanguages: string[] | null;
    requestedLanguage: string;
    isSuccess: boolean;
}

interface SearchParams {
    page: number;
    limit: number;
    lang: string;
    search?: string;
    documentTypes?: string;
}

interface FailedDocument {
    celex: string;
    problem: string;
    availableLanguages: string[];
    originalTitle?: string;
}

// Router
const router = useRouter();

// Check authentication on mount
const checkAuth = () => {
    const token = localStorage.getItem('jwt_token');
    if (!token) {
        router.push('/');
    }
};

// Search and filter state
const selectedLang = ref("EN");
const selectedLawType = ref("");
const search = ref("");

// Pagination state
const currentPage = ref(0);
const limit = ref(20);
const totalDocuments = ref(0);

// Document state
const lawDocuments = ref<LawDocument[]>([]);
const selectedDocuments = ref<Set<string>>(new Set());
const selectAll = ref(false);

// UI state
const loading = ref(false);
const searching = ref(false);
const downloading = ref(false);
const error = ref<string | null>(null);
const initialLoading = ref(true);

// Download progress
const downloadProgress = ref(0);
const estimatedTime = ref(0);
const progressInterval = ref<number | null>(null);
const currentDownloadFile = ref<string>('');
const downloadedFiles = ref(0);
const totalFiles = ref(0);

// Language selection modal
const showLanguageModal = ref(false);
const languageModalData = ref<{
    celex: string;
    title?: string;
    availableLanguages: string[];
    callback?: (lang: string) => void;
} | null>(null);

// Failed documents modal
const showFailedModal = ref(false);
const failedDocuments = ref<FailedDocument[]>([]);
const selectedFailedDocs = ref<Map<string, string>>(new Map());

// Cancellation support
const downloadAbortController = ref<AbortController | null>(null);

// Computed properties
const totalPages = computed(() => Math.ceil(totalDocuments.value / limit.value));
const hasSelectedDocuments = computed(() => selectedDocuments.value.size > 0);
const selectedCount = computed(() => selectedDocuments.value.size);
const hasActiveFilters = computed(() => {
    return search.value.trim() !== '' || selectedLawType.value !== '';
});

const paginationRange = computed(() => {
    const rangeWithDots: (number | string)[] = [];
    
    const current = currentPage.value + 1;
    const total = totalPages.value;
    
    if (total <= 7) {
        for (let i = 1; i <= total; i++) {
            rangeWithDots.push(i);
        }
        return rangeWithDots;
    }
    
    rangeWithDots.push(1);
    
    let start = Math.max(2, current - 2);
    let end = Math.min(total - 1, current + 2);
    
    if (current <= 3) {
        end = 5;
    }
    
    if (current >= total - 2) {
        start = total - 4;
    }
    
    if (start > 2) {
        rangeWithDots.push('...');
    }
    
    for (let i = start; i <= end; i++) {
        rangeWithDots.push(i);
    }
    
    if (end < total - 1) {
        rangeWithDots.push('...');
    }
    
    rangeWithDots.push(total);
    
    return rangeWithDots;
});

// API Functions
const getLawDocumentsAsync = async (
    searchTerm: string | null,
    lawType: string | null,
    lang: string,
    page: number,
    itemLimit: number
): Promise<ApiResponse> => {
    try {
        const params: SearchParams = {
            page,
            limit: itemLimit,
            lang: lang
        };
        
        if (searchTerm && searchTerm.trim()) params.search = searchTerm;
        if (lawType && lawType.trim()) params.documentTypes = lawType;
        
        const response = await axios.get('/api/laws', { params });
        
        if (response.data) {
            if (typeof response.data === 'object' && 'count' in response.data) {
                let documents: LawDocument[] = [];
                
                if (response.data.documents) {
                    documents = response.data.documents;
                } else {
                    for (let key in response.data) {
                        if (key !== 'count' && Array.isArray(response.data[key])) {
                            documents = response.data[key];
                            break;
                        }
                    }
                }
                
                return {
                    count: response.data.count,
                    documents: documents
                };
            }
        }
        
        console.error('Unexpected API response structure:', response.data);
        return { count: 0, documents: [] };
    } catch (error) {
        console.error('Error fetching law documents:', error);
        throw error;
    }
};

const fetchDocuments = async (isNewSearch: boolean = false) => {
    if (isNewSearch) {
        searching.value = true;
    } else {
        loading.value = true;
    }
    error.value = null;
    
    try {
        const response = await getLawDocumentsAsync(
            search.value || null,
            selectedLawType.value || null,
            selectedLang.value,
            currentPage.value,
            limit.value
        );
        
        totalDocuments.value = response.count;
        lawDocuments.value = response.documents;
        
        selectAll.value = false;
    } catch (err: any) {
        if (err.response?.status !== 401) {
            error.value = 'Failed to fetch law documents. Please try again.';
            console.error('Error:', err);
        }
        lawDocuments.value = [];
        totalDocuments.value = 0;
    } finally {
        loading.value = false;
        searching.value = false;
        initialLoading.value = false;
    }
};

const handleSearch = () => {
    currentPage.value = 0;
    fetchDocuments(true);
};

const clearFilters = () => {
    search.value = '';
    selectedLawType.value = '';
    currentPage.value = 0;
    selectedDocuments.value.clear();
    fetchDocuments(true);
};

const openPDF = async (celex: string) => {
    try {
        const response = await axios.get(`/api/laws/${encodeURIComponent(celex)}/pdf`, {
            params: { lang: selectedLang.value },
            timeout: 10000
        });
        
        const data: PdfResponse = response.data;
        
        if (data.isSuccess && data.url) {
            try {
                const url = new URL(data.url);
                if (url.protocol === 'https:') {
                    window.open(data.url, '_blank', 'noopener,noreferrer');
                } else {
                    throw new Error('Invalid URL protocol');
                }
            } catch (urlError) {
                console.error('Invalid URL received:', urlError);
                alert('Failed to open PDF. Invalid URL received.');
            }
        } else if (!data.isSuccess && data.availableLanguages && data.availableLanguages.length > 0) {
            // Show language selection modal
            const doc = lawDocuments.value.find(d => d.celex === celex);
            languageModalData.value = {
                celex: celex,
                title: doc?.title,
                availableLanguages: data.availableLanguages,
                callback: async (lang: string) => {
                    try {
                        const retryResponse = await axios.get(`/api/laws/${encodeURIComponent(celex)}/pdf`, {
                            params: { lang: lang },
                            timeout: 10000
                        });
                        
                        if (retryResponse.data.isSuccess && retryResponse.data.url) {
                            window.open(retryResponse.data.url, '_blank', 'noopener,noreferrer');
                        } else {
                            alert('Failed to open PDF in selected language.');
                        }
                    } catch (err) {
                        console.error('Error fetching PDF:', err);
                        alert('Failed to open PDF. Please try again.');
                    }
                }
            };
            showLanguageModal.value = true;
        } else {
            alert(data.problem || 'This document is not available.');
        }
    } catch (error: any) {
        if (error.response?.status !== 401) {
            console.error('Error fetching PDF URL:', error);
            alert('Failed to open PDF. Please try again.');
        }
    }
};

const selectLanguage = (lang: string) => {
    if (languageModalData.value?.callback) {
        languageModalData.value.callback(lang);
    }
    closeLanguageModal();
};

const closeLanguageModal = () => {
    showLanguageModal.value = false;
    languageModalData.value = null;
};

const downloadSelectedPDFs = async () => {
    if (!hasSelectedDocuments.value) return;
    
    downloading.value = true;
    error.value = null;
    downloadProgress.value = 0;
    downloadedFiles.value = 0;
    currentDownloadFile.value = '';
    failedDocuments.value = [];
    
    // Create new abort controller for this download
    downloadAbortController.value = new AbortController();
    
    try {
        const celexNumbers = Array.from(selectedDocuments.value);
        const validCelexNumbers = celexNumbers.filter(celex => /^[A-Za-z0-9_-]+$/.test(celex));
        
        if (validCelexNumbers.length === 0) {
            throw new Error('No valid documents selected');
        }
        if (validCelexNumbers.length > 200) {
            throw new Error('Too many documents selected (200 max)');
        }
        
        totalFiles.value = validCelexNumbers.length;
        
        // PHASE 1: API Processing
        estimatedTime.value = Math.ceil(validCelexNumbers.length * 2);
        currentDownloadFile.value = 'Processing request...';
        
        // Start progress animation
        let elapsedTime = 0;
        const updateInterval = 800;
        const totalEstimatedMs = estimatedTime.value * 1000;
        
        progressInterval.value = setInterval(() => {
            elapsedTime += updateInterval;
            let progress = Math.floor((elapsedTime / totalEstimatedMs) * 100);
            
            if (progress >= 99) {
                progress = 99;
                if (progressInterval.value) {
                    clearInterval(progressInterval.value);
                    progressInterval.value = null;
                }
            }
            
            downloadProgress.value = progress;
        }, updateInterval);
        
        // Make the API call
        const response = await axios.post('/api/laws/bulk-pdf', 
            validCelexNumbers,
            { 
                headers: {
                    'Content-Type': 'application/json'
                },
                params: {
                    lang: selectedLang.value
                },
                timeout: 600000,
                signal: downloadAbortController.value.signal
            }
        );
        
        // Clear interval if still running
        if (progressInterval.value) {
            clearInterval(progressInterval.value);
            progressInterval.value = null;
        }
        
        downloadProgress.value = 100;
        await new Promise(resolve => setTimeout(resolve, 500));
        
        // Process response
        const pdfResponses: PdfResponse[] = response.data;
        
        // Separate successful and failed documents
        const successfulDocs = pdfResponses.filter(r => r.isSuccess && r.url);
        const failedDocs = pdfResponses.filter(r => !r.isSuccess);
        
        // Store failed documents for later retry
        failedDocuments.value = failedDocs.map(doc => {
            const lawDoc = lawDocuments.value.find(d => d.celex === doc.celex);
            return {
                celex: doc.celex,
                problem: doc.problem || 'Document not available',
                availableLanguages: doc.availableLanguages || [],
                originalTitle: lawDoc?.title
            };
        });
        
        // PHASE 2: Download successful files
        if (successfulDocs.length > 0) {
            downloadProgress.value = 0;
            estimatedTime.value = 0;
            
            const zip = new JSZip();
            let successCount = 0;
            
            for (let i = 0; i < successfulDocs.length; i++) {
                if (downloadAbortController.value.signal.aborted) {
                    throw new Error('Download cancelled');
                }
                
                const doc = successfulDocs[i];
                currentDownloadFile.value = doc.celex;
                
                try {
                    const response = await fetch(doc.url!, { 
                        signal: downloadAbortController.value.signal 
                    });
                    
                    if (!response.ok) {
                        throw new Error(`Failed to download ${doc.celex}`);
                    }
                    
                    const blob = await response.blob();
                    zip.file(`${doc.celex}.pdf`, blob);
                    successCount++;
                    downloadedFiles.value = successCount;
                    downloadProgress.value = Math.round((i + 1) / successfulDocs.length * 100);
                    
                } catch (fileError: any) {
                    console.error(`Failed to download ${doc.celex}:`, fileError);
                    failedDocuments.value.push({
                        celex: doc.celex,
                        problem: 'Download failed',
                        availableLanguages: [],
                        originalTitle: lawDocuments.value.find(d => d.celex === doc.celex)?.title
                    });
                }
            }
            
            if (successCount > 0) {
                downloadProgress.value = 100;
                currentDownloadFile.value = 'Generating ZIP file...';
                
                const zipBlob = await zip.generateAsync({
                    type: 'blob',
                    compression: 'DEFLATE',
                    compressionOptions: { level: 6 }
                });
                
                const timestamp = new Date().toISOString().replace(/[:.]/g, '-').substring(0, 19);
                saveAs(zipBlob, `laws_${timestamp}.zip`);
            }
        }
        
        // Show failed documents modal if any failed
        if (failedDocuments.value.length > 0) {
            showFailedModal.value = true;
            error.value = `Downloaded ${successfulDocs.length} of ${pdfResponses.length} documents. ${failedDocuments.value.length} failed.`;
        }
        
        // Clear selections
        selectedDocuments.value.clear();
        selectAll.value = false;
        
        setTimeout(() => {
            downloadProgress.value = 0;
            estimatedTime.value = 0;
            currentDownloadFile.value = '';
            downloadedFiles.value = 0;
            totalFiles.value = 0;
        }, 2000);
        
    } catch (err: any) {
        if (progressInterval.value) {
            clearInterval(progressInterval.value);
            progressInterval.value = null;
        }
        
        downloadProgress.value = 0;
        estimatedTime.value = 0;
        currentDownloadFile.value = '';
        downloadedFiles.value = 0;
        totalFiles.value = 0;
        
        if (err.message === 'Download cancelled' || err.name === 'AbortError') {
            error.value = 'Download cancelled';
        } else if (err.response?.status === 401) {
            return;
        } else {
            error.value = err.message || 'Failed to download PDFs. Please try again.';
        }
        
        console.error('Error downloading PDFs:', err);
    } finally {
        downloading.value = false;
        downloadAbortController.value = null;
    }
};

const retryFailedDocuments = async () => {
    if (selectedFailedDocs.value.size === 0) {
        alert('Please select at least one document and choose a language for it.');
        return;
    }
    
    showFailedModal.value = false;
    downloading.value = true;
    error.value = null;
    downloadProgress.value = 0;
    
    try {
        const retryRequests: { celex: string; lang: string }[] = [];
        
        selectedFailedDocs.value.forEach((lang, celex) => {
            retryRequests.push({ celex, lang });
        });
        
        currentDownloadFile.value = 'Retrying failed documents...';
        
        const zip = new JSZip();
        let successCount = 0;
        
        for (let i = 0; i < retryRequests.length; i++) {
            const { celex, lang } = retryRequests[i];
            currentDownloadFile.value = celex;
            
            try {
                const response = await axios.get(`/api/laws/${encodeURIComponent(celex)}/pdf`, {
                    params: { lang: lang },
                    timeout: 10000
                });
                
                if (response.data.isSuccess && response.data.url) {
                    const pdfResponse = await fetch(response.data.url);
                    const blob = await pdfResponse.blob();
                    zip.file(`${celex}_${lang}.pdf`, blob);
                    successCount++;
                }
            } catch (err) {
                console.error(`Failed to retry ${celex}:`, err);
            }
            
            downloadProgress.value = Math.round((i + 1) / retryRequests.length * 100);
        }
        
        if (successCount > 0) {
            const zipBlob = await zip.generateAsync({
                type: 'blob',
                compression: 'DEFLATE',
                compressionOptions: { level: 6 }
            });
            
            const timestamp = new Date().toISOString().replace(/[:.]/g, '-').substring(0, 19);
            saveAs(zipBlob, `laws_retry_${timestamp}.zip`);
            
            error.value = `Successfully downloaded ${successCount} of ${retryRequests.length} documents.`;
        } else {
            error.value = 'Failed to download any documents in alternative languages.';
        }
        
    } catch (err) {
        console.error('Error retrying downloads:', err);
        error.value = 'Failed to retry downloads. Please try again.';
    } finally {
        downloading.value = false;
        downloadProgress.value = 0;
        currentDownloadFile.value = '';
        selectedFailedDocs.value.clear();
        failedDocuments.value = [];
    }
};

const closeFailedModal = () => {
    showFailedModal.value = false;
    selectedFailedDocs.value.clear();
    failedDocuments.value = [];
};

const updateFailedDocLanguage = (celex: string, lang: string) => {
    if (lang) {
        selectedFailedDocs.value.set(celex, lang);
    } else {
        selectedFailedDocs.value.delete(celex);
    }
};

const cancelDownload = () => {
    if (downloadAbortController.value) {
        downloadAbortController.value.abort();
        
        if (progressInterval.value) {
            clearInterval(progressInterval.value);
            progressInterval.value = null;
        }
        
        downloading.value = false;
        downloadProgress.value = 0;
        estimatedTime.value = 0;
        currentDownloadFile.value = '';
        downloadedFiles.value = 0;
        totalFiles.value = 0;
        error.value = 'Download cancelled';
        
        downloadAbortController.value = null;
    }
};

const formatTime = (seconds: number): string => {
    if (seconds < 60) {
        return `${seconds} second${seconds !== 1 ? 's' : ''}`;
    }
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    if (remainingSeconds === 0) {
        return `${minutes} minute${minutes !== 1 ? 's' : ''}`;
    }
    return `${minutes} min ${remainingSeconds} sec`;
};

const getLanguageName = (code: string): string => {
    const lang = languages.find(l => l.Code === code);
    return lang ? lang.Name : code;
};

// Selection handlers
const toggleSelectAll = () => {
    if (selectAll.value) {
        lawDocuments.value.forEach(doc => {
            selectedDocuments.value.add(doc.celex);
        });
    } else {
        lawDocuments.value.forEach(doc => {
            selectedDocuments.value.delete(doc.celex);
        });
    }
};

const toggleDocumentSelection = (celex: string) => {
    if (selectedDocuments.value.has(celex)) {
        selectedDocuments.value.delete(celex);
    } else {
        selectedDocuments.value.add(celex);
    }
    
    selectAll.value = lawDocuments.value.length > 0 && 
                     lawDocuments.value.every(doc => selectedDocuments.value.has(doc.celex));
};

const clearAllSelected = () => {
    selectedDocuments.value.clear();
    selectAll.value = false;
};

const isSelected = (celex: string) => selectedDocuments.value.has(celex);

// Pagination handlers
const goToPage = (page: number | string) => {
    if (typeof page === 'number' && page >= 0 && page < totalPages.value) {
        currentPage.value = page;
        fetchDocuments(false);
    }
};

// Utility functions
const getTypeName = (typeCode: string): string => {
    const type = lawTypes.find(lt => lt.Code === typeCode);
    return type ? type.Name : typeCode;
};

const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-EU', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
};

const logout = () => {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user_data');
    localStorage.removeItem('authenticated');
    router.push('/');
};

// Lifecycle
onMounted(() => {
    checkAuth();
    fetchDocuments(false);
});

// Watch for page changes
watch([selectedDocuments], () => {
    selectAll.value = lawDocuments.value.length > 0 && 
                     lawDocuments.value.every(doc => selectedDocuments.value.has(doc.celex));
});
</script>

<template>
    <!-- Loading Overlay -->
    <div v-if="initialLoading || (loading && lawDocuments.length === 0)" class="loading-overlay">
        <div class="loading-spinner-container">
            <div class="loading-spinner"></div>
            <p class="loading-text">Loading documents...</p>
        </div>
    </div>

    <!-- Language Selection Modal -->
    <div v-if="showLanguageModal" class="modal-overlay" @click="closeLanguageModal">
        <div class="modal-content" @click.stop>
            <h3>Document Not Available in {{ selectedLang }}</h3>
            <p v-if="languageModalData?.title" class="modal-doc-title">{{ languageModalData.title }}</p>
            <p class="modal-celex">CELEX: {{ languageModalData?.celex }}</p>
            <p>This document is available in the following languages:</p>
            <div class="language-buttons">
                <button 
                    v-for="lang in languageModalData?.availableLanguages" 
                    :key="lang"
                    @click="selectLanguage(lang)"
                    class="lang-option-btn"
                >
                    {{ getLanguageName(lang) }}
                </button>
            </div>
            <button @click="closeLanguageModal" class="modal-close-btn">Cancel</button>
        </div>
    </div>

    <!-- Failed Documents Modal -->
    <div v-if="showFailedModal" class="modal-overlay" @click="closeFailedModal">
        <div class="modal-content failed-modal" @click.stop>
            <h3>Some Documents Failed to Download</h3>
            <p class="modal-subtitle">The following documents are not available in {{ selectedLang }}. You can select alternative languages for them:</p>
            
            <div class="failed-docs-list">
                <div v-for="doc in failedDocuments" :key="doc.celex" class="failed-doc-item">
                    <div class="failed-doc-info">
                        <span class="doc-celex">{{ doc.celex }}</span>
                        <span v-if="doc.originalTitle" class="doc-title">{{ doc.originalTitle }}</span>
                        <span class="doc-problem">{{ doc.problem }}</span>
                    </div>
                    <div class="lang-selector">
                        <select 
                            v-if="doc.availableLanguages.length > 0"
                            @change="(e) => updateFailedDocLanguage(doc.celex, (e.target as HTMLSelectElement).value)"
                        >
                            <option value="">Select language</option>
                            <option 
                                v-for="lang in doc.availableLanguages" 
                                :key="lang"
                                :value="lang"
                            >
                                {{ getLanguageName(lang) }}
                            </option>
                        </select>
                        <span v-else class="no-langs">No translations available</span>
                    </div>
                </div>
            </div>
            
            <div class="modal-actions">
                <button 
                    @click="retryFailedDocuments" 
                    class="retry-btn"
                    :disabled="selectedFailedDocs.size === 0"
                >
                    Download Selected ({{ selectedFailedDocs.size }})
                </button>
                <button @click="closeFailedModal" class="modal-close-btn">Close</button>
            </div>
        </div>
    </div>

    <!-- Header -->
    <header class="header">
        <h1>Law Picker</h1>
        <button @click="logout" class="logout-btn">Logout</button>
    </header>

    <div class="container">
        <!-- Search Section -->
        <div class="search-section">
            <div class="input-div">
                <input 
                    class="search-input" 
                    type="text" 
                    v-model="search" 
                    placeholder="Search for European laws..."
                    @keyup.enter="handleSearch"
                >
                <select v-model="selectedLawType">
                    <option value="">All Types</option>
                    <option v-for="lawType in lawTypes" :key="lawType.Code" :value="lawType.Code">
                        {{ lawType.Name }}
                    </option>
                </select>
                <select v-model="selectedLang">
                    <option v-for="language in languages" :key="language.Code" :value="language.Code">
                        {{ language.Name }}
                    </option>
                </select>
                <button 
                    type="button" 
                    @click="clearFilters" 
                    :disabled="!hasActiveFilters"
                    class="clear-filters-btn"
                    :class="{ 'active': hasActiveFilters }"
                    title="Clear all filters"
                >
                    <svg width="18" height="18" fill="currentColor" viewBox="0 0 16 16">
                        <path d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z"/>
                    </svg>
                </button>
                <button 
                    type="button" 
                    @click="handleSearch" 
                    :disabled="searching"
                    class="search-btn"
                >
                    {{ searching ? 'Searching...' : 'Search' }}
                </button>
            </div>
        </div>

        <!-- Error Message -->
        <div v-if="error" class="error-message">
            {{ error }}
        </div>

        <!-- Download Progress Message with Cancel Button -->
        <div v-if="downloading" class="download-progress">
            <div class="download-progress-content">
                <div class="mini-spinner"></div>
                <div class="progress-details">
                    <strong>Downloading {{ selectedCount }} files...</strong>
                    
                    <p v-if="estimatedTime > 0">
                        Estimated time: {{ formatTime(estimatedTime) }}
                    </p>
                    <p v-else-if="currentDownloadFile && !currentDownloadFile.includes('Processing')">
                        {{ currentDownloadFile.includes('Generating') || currentDownloadFile.includes('Retrying') 
                           ? currentDownloadFile 
                           : `Downloading: ${currentDownloadFile}` }}
                    </p>
                    <p v-else>
                        Processing request...
                    </p>
                    
                    <p v-if="totalFiles > 0 && downloadedFiles > 0">
                        Progress: {{ downloadedFiles }} / {{ totalFiles }} files
                    </p>
                    
                    <div class="progress-bar-container">
                        <div class="progress-bar">
                            <div 
                                class="progress-bar-fill" 
                                :style="{ width: downloadProgress + '%' }"
                            >
                            </div>
                        </div>
                        <span class="progress-percentage">{{ downloadProgress }}%</span>
                    </div>
                    
                    <p class="progress-status" v-if="downloadProgress > 0 && downloadProgress < 100">
                        <template v-if="estimatedTime > 0">
                            {{ downloadProgress >= 96 ? 'Finalizing request...' : 'Processing files...' }}
                        </template>
                        <template v-else>
                            {{ downloadProgress >= 95 ? 'Finalizing download...' : 'Downloading files from cloud storage...' }}
                        </template>
                    </p>
                </div>
                <button @click="cancelDownload" class="cancel-download-btn">
                    Cancel
                </button>
            </div>
        </div>

        <!-- Action Bar -->
        <div v-if="lawDocuments.length > 0" class="action-bar">
            <div class="selection-info">
                <span class="selected-count">
                    {{ selectedCount }} document{{ selectedCount !== 1 ? 's' : '' }} selected
                </span>
                <button 
                    v-if="selectedCount > 0"
                    @click="clearAllSelected"
                    class="clear-btn"
                >
                    Clear all
                </button>
            </div>
            <button 
                @click="downloadSelectedPDFs" 
                :disabled="!hasSelectedDocuments || downloading"
                class="download-btn"
            >
                <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M.5 9.9a.5.5 0 0 1 .5.5v2.5a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-2.5a.5.5 0 0 1 1 0v2.5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2v-2.5a.5.5 0 0 1 .5-.5z"/>
                    <path d="M7.646 11.854a.5.5 0 0 0 .708 0l3-3a.5.5 0 0 0-.708-.708L8.5 10.293V1.5a.5.5 0 0 0-1 0v8.793L5.354 8.146a.5.5 0 1 0-.708.708l3 3z"/>
                </svg>
                {{ downloading ? 'Processing...' : 'Download Selected PDFs' }}
            </button>
        </div>

        <!-- Results Table -->
        <div v-if="lawDocuments.length > 0" class="results-section">
            <!-- Top Pagination -->
            <div class="pagination">
                <button 
                    @click="goToPage(currentPage - 1)" 
                    :disabled="currentPage === 0 || loading"
                >
                    Previous
                </button>
                <template v-for="page in paginationRange" :key="page">
                    <span v-if="page === '...'" class="ellipsis">...</span>
                    <button 
                        v-else
                        @click="goToPage(Number(page) - 1)"
                        :class="{ active: currentPage === Number(page) - 1 }"
                        :disabled="loading"
                    >
                        {{ page }}
                    </button>
                </template>
                <button 
                    @click="goToPage(currentPage + 1)" 
                    :disabled="currentPage >= totalPages - 1 || loading"
                >
                    Next
                </button>
            </div>

            <div class="table-container" :style="{ opacity: loading ? 0.5 : 1 }">
                <table class="results-table">
                    <thead>
                        <tr>
                            <th>
                                <input 
                                    type="checkbox"
                                    v-model="selectAll"
                                    @change="toggleSelectAll"
                                    class="row-checkbox"
                                    :disabled="loading"
                                >
                            </th>
                            <th>CELEX Number</th>
                            <th>Type</th>
                            <th>Title</th>
                            <th>Date</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="document in lawDocuments" :key="document.id">
                            <td class="checkbox-cell">
                                <input 
                                    type="checkbox"
                                    :checked="isSelected(document.celex)"
                                    @change="toggleDocumentSelection(document.celex)"
                                    @click.stop
                                    class="row-checkbox"
                                    :disabled="loading"
                                >
                            </td>
                            <td>
                                <a 
                                    @click.prevent="!loading && openPDF(document.celex)"
                                    class="celex-cell"
                                    :style="{ pointerEvents: loading ? 'none' : 'auto' }"
                                    href="#"
                                >
                                    {{ document.celex }}
                                </a>
                            </td>
                            <td>{{ getTypeName(document.type) }}</td>
                            <td class="title-cell">{{ document.title }}</td>
                            <td>{{ formatDate(document.date) }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- Bottom Pagination -->
            <div class="pagination">
                <button 
                    @click="goToPage(currentPage - 1)" 
                    :disabled="currentPage === 0 || loading"
                >
                    Previous
                </button>
                <template v-for="page in paginationRange" :key="page">
                    <span v-if="page === '...'" class="ellipsis">...</span>
                    <button 
                        v-else
                        @click="goToPage(Number(page) - 1)"
                        :class="{ active: currentPage === Number(page) - 1 }"
                        :disabled="loading"
                    >
                        {{ page }}
                    </button>
                </template>
                <button 
                    @click="goToPage(currentPage + 1)" 
                    :disabled="currentPage >= totalPages - 1 || loading"
                >
                    Next
                </button>
            </div>
        </div>

        <!-- No Results -->
        <div v-else-if="!loading && !initialLoading" class="no-results">
            <p>No documents found. Try adjusting your search criteria.</p>
        </div>
    </div>
</template>

<style scoped>
/* Loading Overlay */
.loading-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(255, 255, 255, 0.95);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
}

.loading-spinner-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 20px;
}

.loading-spinner {
    width: 50px;
    height: 50px;
    border: 4px solid #f3f3f3;
    border-top: 4px solid #007bff;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

.loading-text {
    color: #495057;
    font-size: 16px;
    margin: 0;
    animation: pulse 1.5s ease-in-out infinite;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.6; }
}

/* Modal Overlay and Content */
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 10000;
    animation: fadeIn 0.2s ease;
}

@keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
}

.modal-content {
    background: white;
    border-radius: 8px;
    padding: 30px;
    max-width: 500px;
    width: 90%;
    max-height: 80vh;
    overflow-y: auto;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
    animation: slideIn 0.3s ease;
}

@keyframes slideIn {
    from {
        opacity: 0;
        transform: translateY(-20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.modal-content h3 {
    margin: 0 0 20px 0;
    color: #212529;
    font-size: 1.5rem;
}

.modal-doc-title {
    color: #495057;
    margin: 10px 0;
    font-size: 14px;
    line-height: 1.4;
}

.modal-celex {
    font-family: 'Courier New', monospace;
    color: #007bff;
    margin: 10px 0;
    font-weight: 500;
}

.modal-subtitle {
    color: #6c757d;
    margin-bottom: 20px;
    font-size: 14px;
}

.language-buttons {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
    gap: 10px;
    margin: 20px 0;
}

.lang-option-btn {
    padding: 10px 15px;
    background-color: #007bff;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    transition: background-color 0.2s;
}

.lang-option-btn:hover {
    background-color: #0056b3;
}

.modal-close-btn {
    width: 100%;
    padding: 10px;
    background-color: #6c757d;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    margin-top: 10px;
    transition: background-color 0.2s;
}

.modal-close-btn:hover {
    background-color: #5a6268;
}

/* Failed Documents Modal */
.failed-modal {
    max-width: 700px;
}

.failed-docs-list {
    max-height: 400px;
    overflow-y: auto;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    padding: 10px;
    margin: 20px 0;
}

.failed-doc-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px;
    border-bottom: 1px solid #e9ecef;
    gap: 15px;
}

.failed-doc-item:last-child {
    border-bottom: none;
}

.failed-doc-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.doc-celex {
    font-family: 'Courier New', monospace;
    font-weight: 600;
    color: #007bff;
}

.doc-title {
    font-size: 13px;
    color: #495057;
    line-height: 1.3;
}

.doc-problem {
    font-size: 12px;
    color: #dc3545;
    font-style: italic;
}

.lang-selector {
    flex-shrink: 0;
}

.lang-selector select {
    padding: 6px 10px;
    border: 1px solid #ced4da;
    border-radius: 4px;
    font-size: 13px;
    cursor: pointer;
    min-width: 150px;
}

.no-langs {
    font-size: 13px;
    color: #6c757d;
    font-style: italic;
}

.modal-actions {
    display: flex;
    gap: 10px;
    margin-top: 20px;
}

.retry-btn {
    flex: 1;
    padding: 10px;
    background-color: #28a745;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    transition: background-color 0.2s;
}

.retry-btn:hover:not(:disabled) {
    background-color: #218838;
}

.retry-btn:disabled {
    background-color: #6c757d;
    cursor: not-allowed;
}

/* Header */
.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 5%;
    background: white;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    margin-bottom: 30px;
}

.header h1 {
    color: #212529;
    font-size: 2rem;
    font-weight: 600;
}

.logout-btn {
    padding: 8px 20px;
    background-color: #dc3545;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    font-weight: 500;
    transition: background-color 0.2s;
}

.logout-btn:hover {
    background-color: #c82333;
}

/* Container */
.container {
    max-width: 1400px;
    margin: 0 auto;
    padding: 0 20px;
}

/* Search Section */
.search-section {
    background: white;
    padding: 25px;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    margin-bottom: 30px;
}

.input-div {
    display: flex;
    gap: 10px;
}

.input-div input,
.input-div select,
.input-div button {
    height: 40px;
    border: 1px solid #ced4da;
    border-radius: 4px;
    font-size: 14px;
}

.search-input {
    flex: 1;
    min-width: 300px;
    padding: 0 12px;
}

.search-input:focus {
    outline: none;
    border-color: #80bdff;
    box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
}

select {
    flex: 0;
    padding: 0 12px;
    background: white;
    cursor: pointer;
    min-width: 120px;
}

.search-btn {
    flex: 0;
    padding: 0 25px;
    background-color: #007bff;
    color: white;
    border: none;
    cursor: pointer;
    font-weight: 500;
    transition: background-color 0.2s;
    min-width: 120px;
    white-space: nowrap;
}

.search-btn:hover:not(:disabled) {
    background-color: #0056b3;
}

.search-btn:disabled {
    background-color: #6c757d;
    cursor: not-allowed;
}

.clear-filters-btn {
    width: 40px;
    height: 40px;
    padding: 0;
    background-color: #e9ecef;
    color: #6c757d;
    border: 1px solid #ced4da;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s;
    flex-shrink: 0;
}

.clear-filters-btn:disabled {
    background-color: #f8f9fa;
    color: #dee2e6;
    cursor: not-allowed;
    border-color: #dee2e6;
}

.clear-filters-btn.active {
    background-color: #dc3545;
    color: white;
    border-color: #dc3545;
}

.clear-filters-btn.active:hover {
    background-color: #c82333;
    border-color: #bd2130;
}

.clear-filters-btn svg {
    flex-shrink: 0;
}

/* Error Message */
.error-message {
    background-color: #f8d7da;
    color: #721c24;
    padding: 12px 20px;
    border: 1px solid #f5c6cb;
    border-radius: 4px;
    margin-bottom: 20px;
}

/* Download Progress Message */
.download-progress {
    background-color: #cce5ff;
    border: 1px solid #b3d7ff;
    color: #004085;
    padding: 15px 20px;
    border-radius: 4px;
    margin-bottom: 20px;
    animation: slideDown 0.3s ease-out;
}

.download-progress-content {
    display: flex;
    gap: 15px;
}

.download-progress p {
    margin: 4px 0 0 0;
    font-size: 14px;
    color: #004085;
}

.mini-spinner {
    margin-top: 12px;
    width: 24px;
    height: 24px;
    border: 3px solid rgba(0, 64, 133, 0.2);
    border-radius: 50%;
    border-top-color: #004085;
    animation: spin 1s linear infinite;
    flex-shrink: 0;
}

.cancel-download-btn {
    height: 30px;
    padding: 6px 16px;
    background-color: #dc3545;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 13px;
    font-weight: 500;
    transition: background-color 0.2s;
    display: flex;
    align-items: center;
    gap: 6px;
    flex-shrink: 0;
}

.cancel-download-btn:hover {
    background-color: #c82333;
}

@keyframes slideDown {
    from {
        opacity: 0;
        transform: translateY(-10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* Action Bar */
.action-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
}

.selection-info {
    display: flex;
    align-items: center;
    gap: 12px;
}

.selected-count {
    color: #6c757d;
    font-size: 14px;
}

.clear-btn {
    padding: 5px 12px;
    background-color: transparent;
    color: #dc3545;
    border: 1px solid #dc3545;
    border-radius: 4px;
    cursor: pointer;
    font-size: 13px;
    font-weight: 500;
    transition: all 0.2s;
}

.clear-btn:hover {
    background-color: #dc3545;
    color: white;
}

.download-btn {
    padding: 8px 20px;
    background-color: #28a745;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 500;
    transition: background-color 0.2s;
    display: flex;
    align-items: center;
    gap: 8px;
}

.download-btn:hover:not(:disabled) {
    background-color: #218838;
}

.download-btn:disabled {
    background-color: #6c757d;
    cursor: not-allowed;
}

/* Results Section */
.results-section {
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    overflow: hidden;
}

.table-container {
    overflow-x: auto;
    transition: opacity 0.3s ease;
}

.results-table {
    width: 100%;
    border-collapse: collapse;
}

.results-table th {
    background-color: #f8f9fa;
    color: #495057;
    font-weight: 600;
    padding: 12px;
    text-align: left;
    border-bottom: 2px solid #dee2e6;
}

.results-table th:first-child {
    width: 50px;
    text-align: center;
}

.results-table td {
    padding: 12px;
    border-bottom: 1px solid #dee2e6;
    vertical-align: middle;
}

.results-table tbody tr {
    transition: background-color 0.2s;
}

.results-table tbody tr:hover {
    background-color: #f8f9fa;
}

.results-table tbody tr:nth-child(even) {
    background-color: #ffffff;
}

.results-table tbody tr:nth-child(odd) {
    background-color: #fafafa;
}

.checkbox-cell {
    text-align: center;
}

.row-checkbox {
    width: 18px;
    height: 18px;
    cursor: pointer;
}

.celex-cell {
    font-family: 'Courier New', monospace;
    font-weight: 500;
    color: #007bff;
    cursor: pointer;
    text-decoration: none;
}

.celex-cell:hover {
    text-decoration: underline;
}

.title-cell {
    max-width: 500px;
    line-height: 1.4;
}

/* Pagination */
.pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 5px;
    padding: 20px;
    background: #f8f9fa;
    border-top: 1px solid #dee2e6;
}

.pagination:first-child {
    border-top: none;
    border-bottom: 1px solid #dee2e6;
}

.pagination button {
    padding: 6px 8px;
    border: 1px solid #dee2e6;
    background: white;
    color: #007bff;
    cursor: pointer;
    border-radius: 4px;
    font-size: 14px;
    min-width: 45px;
    transition: all 0.2s;
    white-space: nowrap;
}

.pagination button:first-child,
.pagination button:last-child {
    min-width: 70px;
    padding: 6px 12px;
}

.pagination button:hover:not(:disabled) {
    background-color: #007bff;
    color: white;
    border-color: #007bff;
}

.pagination button:disabled {
    color: #6c757d;
    cursor: not-allowed;
    background: #e9ecef;
}

.pagination button.active {
    background-color: #007bff;
    color: white;
    border-color: #007bff;
}

.pagination .ellipsis {
    padding: 6px 8px;
    color: #6c757d;
    min-width: 35px;
    text-align: center;
}

/* No Results */
.no-results {
    text-align: center;
    color: #6c757d;
    font-style: italic;
    padding: 60px 20px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

/* Progress Bar */
.progress-details {
    flex: 1;
}

.progress-bar-container {
    margin-top: 12px;
    display: flex;
    align-items: center;
    gap: 12px;
}

.progress-bar {
    flex: 1;
    height: 24px;
    background-color: #e9ecef;
    border-radius: 12px;
    overflow: hidden;
    position: relative;
    box-shadow: inset 0 2px 4px rgba(0,0,0,0.1);
}

.progress-bar-fill {
    height: 100%;
    background: linear-gradient(90deg, #007bff, #0056b3);
    transition: width 0.5s ease;
    border-radius: 12px;
    position: relative;
    overflow: hidden;
    box-shadow: 0 2px 4px rgba(0,0,0,0.2);
}

.progress-bar-fill::after {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
        90deg,
        transparent,
        rgba(255, 255, 255, 0.4),
        transparent
    );
    animation: shimmer 2s infinite;
}

@keyframes shimmer {
    0% {
        transform: translateX(-100%);
    }
    100% {
        transform: translateX(100%);
    }
}

.progress-percentage {
    font-weight: 600;
    color: #004085;
    min-width: 50px;
    text-align: right;
    font-size: 16px;
}

.progress-status {
    margin-top: 8px;
    font-size: 13px;
    color: #004085;
    font-style: italic;
    animation: pulse 1.5s ease-in-out infinite;
}

/* Responsive */
@media (max-width: 768px) {
    .header {
        padding: 15px 3%;
    }
    
    .header h1 {
        font-size: 1.5rem;
    }
    
    .container {
        padding: 0 10px;
    }
    
    .input-div {
        flex-direction: column;
    }
    
    .input-div input,
    .input-div select,
    .input-div button {
        width: 100%;
    }
    
    .action-bar {
        flex-direction: column;
        gap: 10px;
        align-items: stretch;
    }
    
    .selection-info {
        justify-content: center;
    }
    
    .selected-count {
        text-align: center;
    }
    
    .download-progress-content {
        flex-direction: column;
        text-align: center;
        align-items: stretch;
    }
    
    .cancel-download-btn {
        align-self: center;
        margin-top: 10px;
    }
    
    .results-table {
        font-size: 12px;
    }
    
    .results-table th,
    .results-table td {
        padding: 8px;
    }
    
    .title-cell {
        max-width: 250px;
    }
    
    .pagination {
        padding: 15px 10px;
        flex-wrap: wrap;
    }
    
    .pagination button {
        padding: 5px 10px;
        font-size: 12px;
        min-width: 30px;
    }
    
    .loading-spinner {
        width: 40px;
        height: 40px;
    }
    
    .loading-text {
        font-size: 14px;
    }
    
    .progress-bar-container {
        flex-direction: column;
        align-items: stretch;
        gap: 8px;
    }
    
    .progress-percentage {
        text-align: center;
    }
    
    .progress-status {
        text-align: center;
    }
    
    .modal-content {
        width: 95%;
        padding: 20px;
    }
    
    .failed-doc-item {
        flex-direction: column;
        align-items: stretch;
        gap: 10px;
    }
    
    .lang-selector select {
        width: 100%;
    }
    
    .modal-actions {
        flex-direction: column;
    }
}
</style>
