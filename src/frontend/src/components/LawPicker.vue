<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { languages, lawTypes } from '../store/lists';
import axios from 'axios';
import DOMPurify from 'dompurify';  // Add this import for XSS protection

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

interface SearchParams {
    page: number;
    limit: number;
    search?: string;
    documentTypes?: string;
}

// Router
const router = useRouter();

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
const searching = ref(false);  // Separate state for search button
const downloading = ref(false);
const error = ref<string | null>(null);

// Computed properties
const totalPages = computed(() => Math.ceil(totalDocuments.value / limit.value));
const hasSelectedDocuments = computed(() => selectedDocuments.value.size > 0);
const selectedCount = computed(() => selectedDocuments.value.size);

const paginationRange = computed(() => {
    const rangeWithDots: (number | string)[] = [];
    
    // Convert 0-indexed currentPage to 1-indexed for display
    const current = currentPage.value + 1;
    const total = totalPages.value;
    
    // If total pages is 7 or less, show all pages
    if (total <= 7) {
        for (let i = 1; i <= total; i++) {
            rangeWithDots.push(i);
        }
        return rangeWithDots;
    }
    
    // Always show first page
    rangeWithDots.push(1);
    
    // Calculate the range around current page
    let start = Math.max(2, current - 2);
    let end = Math.min(total - 1, current + 2);
    
    // Adjust if we're near the beginning
    if (current <= 3) {
        end = 5;
    }
    
    // Adjust if we're near the end
    if (current >= total - 2) {
        start = total - 4;
    }
    
    // Add ellipsis after 1 if needed
    if (start > 2) {
        rangeWithDots.push('...');
    }
    
    // Add the middle pages
    for (let i = start; i <= end; i++) {
        rangeWithDots.push(i);
    }
    
    // Add ellipsis before last page if needed
    if (end < total - 1) {
        rangeWithDots.push('...');
    }
    
    // Always show last page
    rangeWithDots.push(total);
    
    return rangeWithDots;
});

// API Functions
const getLawDocumentsAsync = async (
    searchTerm: string | null,
    lawType: string | null,
    page: number,
    itemLimit: number
): Promise<ApiResponse> => {
    try {
        const params: SearchParams = {
            page,
            limit: itemLimit
        };
        
        if (searchTerm && searchTerm.trim()) params.search = searchTerm;
        if (lawType && lawType.trim()) params.documentTypes = lawType;
        
        const response = await axios.get('https://localhost:8000/api/laws', { params });
        
        console.log('Raw API response:', response.data); // Debug log
        
        // Based on your description, API returns { count: number, [array of documents] }
        // But looking at your sample data, it seems to be just an array
        if (response.data) {
            // If it's an object with count and an array at index 0 or 1
            if (typeof response.data === 'object' && 'count' in response.data) {
                // Find the documents array in the response
                let documents: LawDocument[] = [];
                
                // Check if documents is a direct property
                if (response.data.documents) {
                    documents = response.data.documents;
                } else {
                    // Check numeric keys (0, 1, etc.)
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
            
            // If it's just an array (like your sample data)
            if (Array.isArray(response.data)) {
                return {
                    count: response.data.length,
                    documents: response.data
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
            currentPage.value,
            limit.value
        );
        
        console.log('API Response:', response); // Debug log
        
        totalDocuments.value = response.count;
        lawDocuments.value = response.documents;
        
        // Don't clear selections when just changing pages
        // Only clear on new search
        selectAll.value = false;
    } catch (err) {
        error.value = 'Failed to fetch law documents. Please try again.';
        console.error('Error:', err);
        lawDocuments.value = [];
        totalDocuments.value = 0;
    } finally {
        loading.value = false;
        searching.value = false;
    }
};

const handleSearch = () => {
    currentPage.value = 0; // Reset to first page on new search
    selectedDocuments.value.clear(); // Clear selections only on new search
    fetchDocuments(true);  // Pass true to indicate this is a search
};

const openPDF = async (celex: string) => {
    try {
        // Validate CELEX number format (basic validation)
        if (!/^[A-Za-z0-9_-]+$/.test(celex)) {
            console.error('Invalid CELEX format');
            alert('Invalid document identifier');
            return;
        }
        
        const response = await axios.get(`https://localhost:8000/api/laws/${encodeURIComponent(celex)}/pdf`, {
            params: { lang: selectedLang.value },
            timeout: 10000,
            withCredentials: true
        });
        
        if (response.data && response.data.url) {
            // Validate URL before opening
            try {
                const url = new URL(response.data.url);
                // Only allow https URLs from trusted domains
                if (url.protocol === 'https:' || url.protocol === 'http:') {
                    window.open(response.data.url, '_blank', 'noopener,noreferrer');
                } else {
                    throw new Error('Invalid URL protocol');
                }
            } catch (urlError) {
                console.error('Invalid URL received:', urlError);
                alert('Failed to open PDF. Invalid URL received.');
            }
        }
    } catch (error) {
        console.error('Error fetching PDF URL:', error);
        alert('Failed to open PDF. Please try again.');
    }
};

const downloadSelectedPDFs = async () => {
    if (!hasSelectedDocuments.value) return;
    
    downloading.value = true;
    error.value = null;
    
    try {
        const celexNumbers = Array.from(selectedDocuments.value);
        
        // Validate all CELEX numbers
        const validCelexNumbers = celexNumbers.filter(celex => /^[A-Za-z0-9_-]+$/.test(celex));
        
        if (validCelexNumbers.length === 0) {
            throw new Error('No valid documents selected');
        }
        
        // Limit the number of documents to prevent abuse
        const limitedCelexNumbers = validCelexNumbers.slice(0, 100);
        
        if (limitedCelexNumbers.length < validCelexNumbers.length) {
            error.value = `Downloading first 100 documents of ${validCelexNumbers.length} selected`;
        }
        
        const response = await axios.post('https://localhost:8000/api/laws/pdf', 
            limitedCelexNumbers,
            { 
                responseType: 'blob',
                headers: {
                    'Content-Type': 'application/json'
                },
                params: {
                    lang: selectedLang.value
                },
                timeout: 300000, // 5 minute timeout for large downloads
                withCredentials: true,
                maxContentLength: 500 * 1024 * 1024, // 500MB max
                validateStatus: (status) => status >= 200 && status < 300
            }
        );
        
        // Validate response type
        if (response.data.type !== 'application/zip' && !response.data.type.includes('zip')) {
            console.warn('Unexpected response type:', response.data.type);
        }
        
        // Create blob link to download
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').substring(0, 19);
        link.setAttribute('download', `laws_${timestamp}.zip`);
        document.body.appendChild(link);
        link.click();
        link.remove();
        
        // Clean up URL after a delay
        setTimeout(() => window.URL.revokeObjectURL(url), 100);
        
        // Clear selections after download
        selectedDocuments.value.clear();
        selectAll.value = false;
    } catch (err: any) {
        if (err.code === 'ECONNABORTED') {
            error.value = 'Download timeout. Try selecting fewer documents.';
        } else if (err.response?.status === 413) {
            error.value = 'Too many documents selected. Please select fewer documents.';
        } else {
            error.value = 'Failed to download PDFs. Please try again.';
        }
        console.error('Error downloading PDFs:', err);
    } finally {
        downloading.value = false;
    }
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
    
    // Update select all checkbox state
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
        fetchDocuments(false);  // Pass false for pagination
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
    localStorage.clear();
    router.push('/');
};

// Lifecycle
onMounted(() => {
    fetchDocuments(false);  // Initial load is not a search
});

// Watch for page changes
watch([selectedDocuments], () => {
    selectAll.value = lawDocuments.value.length > 0 && 
                     lawDocuments.value.every(doc => selectedDocuments.value.has(doc.celex));
});
</script>

<template>
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

        <!-- Download Progress Message -->
        <div v-if="downloading" class="download-progress">
            <div class="download-progress-content">
                <div class="mini-spinner"></div>
                <div>
                    <strong>Downloading files...</strong>
                    <p>This might take some time depending on the number of documents selected.</p>
                </div>
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
        <div v-else-if="!loading" class="no-results">
            <p>No documents found. Try adjusting your search criteria.</p>
        </div>
    </div>

    <!-- No Loading Overlay - Removed -->
</template>

<style scoped>
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
    padding: 0 12px;
    background: white;
    cursor: pointer;
    min-width: 120px;
}

.search-btn {
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
    align-items: center;
    gap: 15px;
}

.download-progress p {
    margin: 4px 0 0 0;
    font-size: 14px;
    color: #004085;
}

.mini-spinner {
    width: 24px;
    height: 24px;
    border: 3px solid rgba(0, 64, 133, 0.2);
    border-radius: 50%;
    border-top-color: #004085;
    animation: spin 1s linear infinite;
    flex-shrink: 0;
}

@keyframes spin {
    to { transform: rotate(360deg); }
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

/* Table container opacity transition */
.table-container {
    transition: opacity 0.3s ease;
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
}
</style>