import React, { useState } from 'react';
import { FaceAuthModal } from './components/FaceAuthModal';
import { ATSScorecard } from './components/ATSScorecard';
import { FileUp, ScanFace, FileText, CheckCircle2 } from 'lucide-react';

export default function App() {
  const [activeTab, setActiveTab] = useState<'auth' | 'upload' | 'results'>('auth');
  const [userId, setUserId] = useState<number | null>(null);

  // Mock score result matching Python calculate_ats_score logic
  const [report] = useState({
    name: "Alex Morgan",
    email: "alex.morgan@example.com",
    phone: "+91 9876543210",
    score: 87,
    breakdown: {
      contact_information: 10,
      resume_sections: 15,
      technical_skills: 25,
      projects: 15,
      experience: 10,
      certifications: 7,
      achievements: 5,
    },
    skills: ["Python", "FastAPI", "ASP.NET Core", "SQL", "React", "Docker", "Git", "OpenCV", "REST APIs", "Pandas"]
  });

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col items-center py-10 px-4">
      <header className="text-center mb-8">
        <h1 className="text-3xl font-extrabold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
          AI Resume & ATS Analytics Platform
        </h1>
        <p className="text-xs text-slate-400 mt-1">Biometric Verification & Intelligent Parser Engine</p>
      </header>

      {/* Navigation Tabs */}
      <div className="flex bg-slate-900 border border-slate-800 p-1 rounded-xl mb-8">
        <button
          onClick={() => setActiveTab('auth')}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition ${
            activeTab === 'auth' ? 'bg-indigo-600 text-white' : 'text-slate-400 hover:text-white'
          }`}
        >
          <ScanFace size={15}/> 1. Face ID Login
        </button>
        <button
          onClick={() => setActiveTab('upload')}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition ${
            activeTab === 'upload' ? 'bg-indigo-600 text-white' : 'text-slate-400 hover:text-white'
          }`}
        >
          <FileUp size={15}/> 2. Upload Resume
        </button>
        <button
          onClick={() => setActiveTab('results')}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-semibold transition ${
            activeTab === 'results' ? 'bg-indigo-600 text-white' : 'text-slate-400 hover:text-white'
          }`}
        >
          <FileText size={15}/> 3. ATS Scorecard
        </button>
      </div>

      {/* Main Viewport Container */}
      <main className="w-full flex justify-center">
        {activeTab === 'auth' && (
          <FaceAuthModal
            mode="login"
            onSuccess={(id) => {
              setUserId(id || 1);
              setActiveTab('upload');
            }}
          />
        )}

        {activeTab === 'upload' && (
          <div className="bg-slate-900 border border-dashed border-slate-700 hover:border-indigo-500/50 p-12 rounded-2xl max-w-lg w-full text-center cursor-pointer transition">
            <FileUp size={40} className="mx-auto text-indigo-400 mb-4" />
            <h3 className="font-bold text-white text-base">Upload Candidate Resume (PDF)</h3>
            <p className="text-xs text-slate-400 mt-1 mb-6">Supports PyMuPDF visual coordinate mapping & ATS extraction</p>
            <button
              onClick={() => setActiveTab('results')}
              className="px-6 py-2.5 bg-indigo-600 hover:bg-indigo-500 rounded-xl text-xs font-semibold text-white shadow-lg shadow-indigo-600/30 transition"
            >
              Analyze Resume
            </button>
          </div>
        )}

        {activeTab === 'results' && (
          <ATSScorecard
            score={report.score}
            breakdown={report.breakdown}
            skills={report.skills}
            name={report.name}
            email={report.email}
            phone={report.phone}
          />
        )}
      </main>
    </div>
  );
}
